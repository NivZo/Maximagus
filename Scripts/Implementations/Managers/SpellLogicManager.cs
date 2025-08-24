using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;
using Scripts.State;

namespace Maximagus.Scripts.Managers
{

    public static class SpellLogicManager
    {
        /// <summary>
        /// Pre-calculates an action with complete EncounterState snapshot creation.
        /// This creates a complete snapshot containing both the action result and the resulting encounter state.
        /// </summary>
        public static EncounterStateSnapshot PreCalculateActionWithSnapshot(
            ActionResource action,
            EncounterState currentEncounterState)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (currentEncounterState == null)
                throw new ArgumentNullException(nameof(currentEncounterState));

            // Calculate the action result using the encounter state
            var actionResult = PreCalculateActionResult(action, currentEncounterState);
            
            // Simulate the action's effects on the encounter state
            var resultingEncounterState = SimulateActionEffectsOnEncounterState(
                currentEncounterState, 
                action, 
                actionResult);
            
            // Create the action key using both action ID and current action index for uniqueness
            var actionKey = $"{action.ActionId}_{currentEncounterState.ActionIndex}";
            
            // Create and return the snapshot
            return EncounterStateSnapshot.Create(actionKey, resultingEncounterState, actionResult);
        }

        /// <summary>
        /// Pre-calculates all actions in a spell sequence, generating complete EncounterState snapshots.
        /// Each snapshot contains the complete encounter state after that action executes.
        /// </summary>
        public static ImmutableArray<EncounterStateSnapshot> PreCalculateSpellWithSnapshots(
            IGameStateData initialGameState,
            IEnumerable<CardState> playedCards)
        {
            if (initialGameState == null)
                throw new ArgumentNullException(nameof(initialGameState));
            if (playedCards == null)
                throw new ArgumentNullException(nameof(playedCards));

            var snapshots = ImmutableArray.CreateBuilder<EncounterStateSnapshot>();
            var currentEncounterState = EncounterState.FromGameState(initialGameState, DateTime.UtcNow);
            var actionIndex = 0;
            
            var allActions = playedCards.SelectMany(c => c.Resource.Actions ?? Enumerable.Empty<ActionResource>()).ToList();
            GD.Print($"[SpellLogicManager] Pre-calculating spell with {allActions.Count} actions using EncounterState snapshots");

            foreach (var playedCard in playedCards)
            {
                if (playedCard.Resource.Actions == null) continue;
                
                foreach (var action in playedCard.Resource.Actions)
                {
                    GD.Print("\n-----------------------------------------------------------------------");
                    
                    // Update the encounter state with the current action index
                    currentEncounterState = currentEncounterState.WithActionIndex(actionIndex);
                    
                    // Pre-calculate this action with complete snapshot
                    var snapshot = PreCalculateActionWithSnapshot(action, currentEncounterState);
                    snapshots.Add(snapshot);
                    
                    // Use the resulting state for the next action
                    currentEncounterState = snapshot.ResultingState;
                    
                    GD.Print($"[SpellLogicManager] Created snapshot for action {actionIndex}: {action.GetType().Name} -> {snapshot.ActionResult.FinalDamage} damage");
                    GD.Print("-----------------------------------------------------------------------");
                    
                    actionIndex++;
                }
            }

            GD.Print($"[SpellLogicManager] Completed spell pre-calculation with {snapshots.Count} snapshots");
            return snapshots.ToImmutable();
        }

        /// <summary>
        /// Applies a pre-calculated EncounterState snapshot to the current game state.
        /// This updates both spell state and status effect state atomically.
        /// Preserves important spell properties like SnapshotSpellId that are needed for subsequent actions.
        /// </summary>
        public static IGameStateData ApplyEncounterSnapshot(
            IGameStateData gameState,
            EncounterStateSnapshot snapshot)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (!snapshot.IsValid())
                throw new ArgumentException("Invalid snapshot provided", nameof(snapshot));

            GD.Print($"[SpellLogicManager] Applying encounter snapshot: {snapshot.ActionKey}");
            
            // Get the spell state from the snapshot
            var snapshotSpellState = snapshot.ResultingState.Spell;
            
            // Preserve important properties from the current spell state that aren't in the snapshot
            var currentSpellState = gameState.Spell;
            var preservedProperties = snapshotSpellState.Properties;
            
            // Preserve the SnapshotSpellId if it exists in the current state but not in the snapshot
            if (currentSpellState.Properties.TryGetValue("SnapshotSpellId", out var spellIdValue) &&
                !snapshotSpellState.Properties.ContainsKey("SnapshotSpellId"))
            {
                preservedProperties = preservedProperties.SetItem("SnapshotSpellId", spellIdValue);
            }
            
            // Create the updated spell state with preserved properties
            var updatedSpellState = new SpellState(
                snapshotSpellState.IsActive,
                preservedProperties,
                snapshotSpellState.ActiveModifiers,
                snapshotSpellState.TotalDamageDealt,
                snapshotSpellState.History,
                snapshotSpellState.StartTime,
                snapshotSpellState.CurrentActionIndex);
            
            // Apply the updated spell state and status effects from the snapshot
            return gameState
                .WithSpell(updatedSpellState)
                .WithStatusEffects(snapshot.ResultingState.StatusEffects);
        }

        /// <summary>
        /// Pre-calculates the execution result for an action without applying it to the game state.
        /// This is used for UI previews and to ensure consistency between preview and actual execution.
        /// </summary>
        public static ActionExecutionResult PreCalculateActionResult(
            ActionResource action,
            IGameStateData gameState)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            switch (action)
            {
                case DamageActionResource damageAction:
                    var (finalDamage, consumedModifiers) = ApplyDamageModifiers(damageAction, gameState);
                    return ActionExecutionResult.CreateForDamage(damageAction, finalDamage, consumedModifiers);
                    
                default:
                    return ActionExecutionResult.CreateForNonDamage(action);
            }
        }

        /// <summary>
        /// Pre-calculates the execution result for an action using EncounterState.
        /// This overload works with EncounterState for snapshot-based pre-calculation.
        /// </summary>
        public static ActionExecutionResult PreCalculateActionResult(
            ActionResource action,
            EncounterState encounterState)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            if (encounterState == null)
                throw new ArgumentNullException(nameof(encounterState));

            switch (action)
            {
                case DamageActionResource damageAction:
                    var (finalDamage, consumedModifiers) = ApplyDamageModifiers(damageAction, encounterState);
                    return ActionExecutionResult.CreateForDamage(damageAction, finalDamage, consumedModifiers);
                    
                default:
                    return ActionExecutionResult.CreateForNonDamage(action);
            }
        }






        public static (float finalDamage, ImmutableArray<ModifierData> remainingModifiers) ApplyDamageModifiers(
            DamageActionResource damageAction,
            IGameStateData gameState)
        {
            if (damageAction == null)
                throw new ArgumentNullException(nameof(damageAction));
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));

            var activeModifiers = gameState.Spell.ActiveModifiers;
            GD.Print($"[Spell Casting Refactor] ApplyDamageModifiers: {damageAction.DamageType} damage {damageAction.Amount} with {activeModifiers.Length} modifiers");

            var baseDamage = GetRawDamage(damageAction, gameState);
            var modifiedDamage = baseDamage;
            var modifiersToRemove = ImmutableArray.CreateBuilder<ModifierData>();

            foreach (var modifier in activeModifiers)
            {
                if (modifier.CanApply(damageAction.DamageType))
                {
                    var oldDamage = modifiedDamage;
                    modifiedDamage = modifier.Apply(modifiedDamage);
                    GD.Print($"[Spell Casting Refactor] Applied modifier {modifier.Type} {modifier.Value}: {oldDamage} -> {modifiedDamage}");
                    
                    if (modifier.IsConsumedOnUse)
                    {
                        modifiersToRemove.Add(modifier);
                        GD.Print($"[Spell Casting Refactor] Marked consumable modifier for removal: {modifier.Type} {modifier.Value}");
                    }
                }
            }

            var remainingModifiers = activeModifiers;
            foreach (var modifierToRemove in modifiersToRemove)
            {
                var index = remainingModifiers.IndexOf(modifierToRemove);
                if (index >= 0)
                {
                    remainingModifiers = remainingModifiers.RemoveAt(index);
                }
            }

            GD.Print($"[Spell Casting Refactor] Final damage: {modifiedDamage}, Consumed {modifiersToRemove.Count} modifiers, {remainingModifiers.Length} remaining");
            return (modifiedDamage, remainingModifiers);
        }

        /// <summary>
        /// Applies damage modifiers using EncounterState for snapshot-based calculations.
        /// This includes status effect stacks in damage modifier calculations.
        /// </summary>
        public static (float finalDamage, ImmutableArray<ModifierData> remainingModifiers) ApplyDamageModifiers(
            DamageActionResource damageAction,
            EncounterState encounterState)
        {
            if (damageAction == null)
                throw new ArgumentNullException(nameof(damageAction));
            if (encounterState == null)
                throw new ArgumentNullException(nameof(encounterState));

            var activeModifiers = encounterState.Spell.ActiveModifiers;
            GD.Print($"[EncounterState] ApplyDamageModifiers: {damageAction.DamageType} damage {damageAction.Amount} with {activeModifiers.Length} modifiers");

            var baseDamage = GetRawDamage(damageAction, encounterState);
            var modifiedDamage = baseDamage;
            var modifiersToRemove = ImmutableArray.CreateBuilder<ModifierData>();

            foreach (var modifier in activeModifiers)
            {
                if (modifier.CanApply(damageAction.DamageType))
                {
                    var oldDamage = modifiedDamage;
                    modifiedDamage = modifier.Apply(modifiedDamage);
                    GD.Print($"[EncounterState] Applied modifier {modifier.Type} {modifier.Value}: {oldDamage} -> {modifiedDamage}");
                    
                    if (modifier.IsConsumedOnUse)
                    {
                        modifiersToRemove.Add(modifier);
                        GD.Print($"[EncounterState] Marked consumable modifier for removal: {modifier.Type} {modifier.Value}");
                    }
                }
            }

            var remainingModifiers = activeModifiers;
            foreach (var modifierToRemove in modifiersToRemove)
            {
                var index = remainingModifiers.IndexOf(modifierToRemove);
                if (index >= 0)
                {
                    remainingModifiers = remainingModifiers.RemoveAt(index);
                }
            }

            GD.Print($"[EncounterState] Final damage: {modifiedDamage}, Consumed {modifiersToRemove.Count} modifiers, {remainingModifiers.Length} remaining");
            return (modifiedDamage, remainingModifiers);
        }


        public static SpellState AddModifier(SpellState currentState, ModifierData modifier)
        {
            if (currentState == null)
                throw new ArgumentNullException(nameof(currentState));
            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            GD.Print($"[Spell Casting Refactor] AddModifier: Adding {modifier.Type} {modifier.Value} {modifier.Element} modifier");
            return currentState.WithAddedModifier(modifier);
        }


        public static SpellState UpdateProperty(
            SpellState currentState,
            string key,
            Variant value,
            ContextPropertyOperation operation)
        {
            if (currentState == null)
                throw new ArgumentNullException(nameof(currentState));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            var currentValue = currentState.GetProperty(key, 0f);
            var floatValue = value.AsSingle();

            var newValue = operation switch
            {
                ContextPropertyOperation.Add => currentValue + floatValue,
                ContextPropertyOperation.Multiply => currentValue * floatValue,
                ContextPropertyOperation.Set => floatValue,
                _ => currentValue
            };

            GD.Print($"[Spell Casting Refactor] UpdateProperty: {key} {operation} {floatValue} ({currentValue} -> {newValue})");
            return currentState.WithProperty(key, Variant.From(newValue));
        }

        /// <summary>
        /// Updates a property in the spell state using the ContextProperty enum.
        /// </summary>
        /// <param name="currentState">The current spell state</param>
        /// <param name="property">The context property to update</param>
        /// <param name="value">The value to apply</param>
        /// <param name="operation">The operation to perform (Add, Multiply, Set)</param>
        /// <returns>New spell state with the updated property</returns>
        public static SpellState UpdateProperty(
            SpellState currentState,
            ContextProperty property,
            float value,
            ContextPropertyOperation operation)
        {
            return UpdateProperty(currentState, property.ToString(), Variant.From(value), operation);
        }

        /// <summary>
        /// Processes a damage action and updates the spell state with the results.
        /// This includes applying modifiers, updating total damage, and updating context properties.
        /// </summary>
        /// <param name="gameState">The complete game state for accessing spell and status effect data</param>
        /// <param name="damageAction">The damage action to process</param>
        /// <returns>New spell state with damage processing results</returns>
        /// <summary>
        /// Processes a damage action. This method is deprecated and should not be used.
        /// Use EncounterState snapshot-based execution instead via ApplyEncounterSnapshot.
        /// </summary>
        [Obsolete("Use EncounterState snapshot-based execution instead via ApplyEncounterSnapshot")]
        public static SpellState ProcessDamageAction(
            IGameStateData gameState,
            DamageActionResource damageAction)
        {
            throw new InvalidOperationException(
                "ProcessDamageAction is deprecated. Use EncounterState snapshot-based execution instead. " +
                "Ensure PreCalculateSpellCommand creates snapshots and ExecuteCardActionCommand uses ApplyEncounterSnapshot.");
        }

        /// <summary>
        /// Gets the raw damage value for a damage action, accounting for special damage types like PerChill.
        /// </summary>
        /// <param name="damageAction">The damage action</param>
        /// <param name="gameState">The complete game state for accessing status effects</param>
        /// <returns>The raw damage value</returns>
        private static float GetRawDamage(DamageActionResource damageAction, IGameStateData gameState)
        {
            return damageAction.DamageType switch
            {
                DamageType.None => damageAction.Amount,
                DamageType.Fire => damageAction.Amount,
                DamageType.Frost => damageAction.Amount,
                DamageType.PerChill => damageAction.Amount * StatusEffectLogicManager.GetStacksOfEffect(gameState.StatusEffects, StatusEffectType.Chill),
                _ => damageAction.Amount
            };
        }

        /// <summary>
        /// Gets the raw damage value for a damage action using EncounterState, accounting for special damage types like PerChill.
        /// This uses status effect state from the EncounterState for accurate PerChill calculations.
        /// </summary>
        /// <param name="damageAction">The damage action</param>
        /// <param name="encounterState">The encounter state for accessing status effects</param>
        /// <returns>The raw damage value</returns>
        private static float GetRawDamage(DamageActionResource damageAction, EncounterState encounterState)
        {
            return damageAction.DamageType switch
            {
                DamageType.None => damageAction.Amount,
                DamageType.Fire => damageAction.Amount,
                DamageType.Frost => damageAction.Amount,
                DamageType.PerChill => damageAction.Amount * StatusEffectLogicManager.GetStacksOfEffect(encounterState.StatusEffects, StatusEffectType.Chill),
                _ => damageAction.Amount
            };
        }

        /// <summary>
        /// Simulates an action's effects on the EncounterState for pre-calculation purposes.
        /// This creates a new EncounterState with all the changes that would result from executing the action.
        /// </summary>
        private static EncounterState SimulateActionEffectsOnEncounterState(
            EncounterState currentEncounterState,
            ActionResource action,
            ActionExecutionResult actionResult)
        {
            switch (action)
            {
                case DamageActionResource damageAction:
                    // Update spell state with consumed modifiers and context properties
                    var currentSpellState = currentEncounterState.Spell;
                    var remainingModifiers = currentSpellState.ActiveModifiers;
                    
                    // Remove consumed modifiers
                    foreach (var consumedModifier in actionResult.ConsumedModifiers)
                    {
                        var index = remainingModifiers.IndexOf(consumedModifier);
                        if (index >= 0)
                        {
                            remainingModifiers = remainingModifiers.RemoveAt(index);
                        }
                    }
                    
                    // Update modifiers, total damage, and action index
                    var newTotalDamage = currentSpellState.TotalDamageDealt + actionResult.FinalDamage;
                    var newActionIndex = currentEncounterState.ActionIndex + 1;
                    var newSpellState = currentSpellState
                        .WithModifiers(remainingModifiers)
                        .WithTotalDamage(newTotalDamage)
                        .WithActionIndex(newActionIndex);
                    
                    // Update context properties
                    if (actionResult.FinalDamage > 0)
                    {
                        var damageDealtProperty = damageAction.DamageType switch
                        {
                            DamageType.Fire => ContextProperty.FireDamageDealt,
                            DamageType.Frost => ContextProperty.FrostDamageDealt,
                            DamageType.PerChill => ContextProperty.FrostDamageDealt,
                            _ => throw new ArgumentException($"No context property implemented for damage type {damageAction.DamageType}")
                        };
                        newSpellState = UpdateProperty(newSpellState, damageDealtProperty, actionResult.FinalDamage, ContextPropertyOperation.Add);
                    }
                    
                    // Update both spell state and action index in encounter state
                    return currentEncounterState.WithSpell(newSpellState).WithActionIndex(newActionIndex);
                    
                case ModifierActionResource modifierAction:
                    var modifierData = ModifierData.FromActionResource(modifierAction);
                    var spellStateWithModifier = AddModifier(currentEncounterState.Spell, modifierData);
                    var newActionIndexForModifier = currentEncounterState.ActionIndex + 1;
                    var updatedSpellStateWithModifier = spellStateWithModifier.WithActionIndex(newActionIndexForModifier);
                    return currentEncounterState.WithSpell(updatedSpellStateWithModifier).WithActionIndex(newActionIndexForModifier);
                    
                case StatusEffectActionResource statusEffectAction:
                    var newStatusEffectsState = StatusEffectLogicManager.ApplyStatusEffectToEncounter(
                        currentEncounterState,
                        statusEffectAction.StatusEffect,
                        statusEffectAction.Stacks,
                        statusEffectAction.ActionType);
                    var newActionIndexForStatus = currentEncounterState.ActionIndex + 1;
                    var updatedSpellStateWithStatus = newStatusEffectsState.Spell.WithActionIndex(newActionIndexForStatus);
                    return newStatusEffectsState.WithSpell(updatedSpellStateWithStatus).WithActionIndex(newActionIndexForStatus);
                    
                default:
                    var newActionIndexDefault = currentEncounterState.ActionIndex + 1;
                    var updatedSpellStateDefault = currentEncounterState.Spell.WithActionIndex(newActionIndexDefault);
                    return currentEncounterState.WithSpell(updatedSpellStateDefault).WithActionIndex(newActionIndexDefault);
            }
        }
    }
}