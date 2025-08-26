using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;
using Scripts.State;
using Scripts.Utils;

namespace Maximagus.Scripts.Managers
{

    public static class SpellLogicManager
    {
        private static readonly ILogger _logger = ServiceLocator.GetService<ILogger>();

        private static EncounterStateSnapshot PreCalculateActionWithSnapshot(
            ActionResource action,
            EncounterState currentEncounterState)
        {
            CommonValidation.ThrowIfNull(action, nameof(action));
            CommonValidation.ThrowIfNull(currentEncounterState, nameof(currentEncounterState));

            var actionResult = PreCalculateActionResult(action, currentEncounterState);
            
            var resultingEncounterState = SimulateActionEffectsOnEncounterState(
                currentEncounterState,
                action,
                actionResult);
            
            var actionKey = $"{action.ActionId}_{currentEncounterState.ActionIndex}";
            
            return EncounterStateSnapshot.Create(actionKey, resultingEncounterState, actionResult);
        }

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
            _logger.LogInfo($"[SpellLogicManager] Pre-calculating spell with {allActions.Count} actions using EncounterState snapshots");

            foreach (var playedCard in playedCards)
            {
                if (playedCard.Resource.Actions == null) continue;
                
                foreach (var action in playedCard.Resource.Actions)
                {
                    _logger.LogInfo("\n-----------------------------------------------------------------------");
                    
                    currentEncounterState = currentEncounterState.WithActionIndex(actionIndex);
                    
                    var snapshot = PreCalculateActionWithSnapshot(action, currentEncounterState);
                    snapshots.Add(snapshot);
                    
                    currentEncounterState = snapshot.ResultingState;
                    
                    _logger.LogInfo($"[SpellLogicManager] Created snapshot for action {actionIndex}: {action.GetType().Name} -> {snapshot.ActionResult.FinalDamage} damage");
                    _logger.LogInfo("-----------------------------------------------------------------------");
                    
                    actionIndex++;
                }
            }

            _logger.LogInfo($"[SpellLogicManager] Completed spell pre-calculation with {snapshots.Count} snapshots");
            return snapshots.ToImmutable();
        }

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

            _logger.LogInfo($"[SpellLogicManager] Applying encounter snapshot: {snapshot.ActionKey}");
            
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
                    var (finalDamage, consumedModifiers, _) = ApplyDamageModifiers(damageAction, encounterState);
                    return ActionExecutionResult.Create(damageAction, finalDamage, consumedModifiers);

                default:
                    return ActionExecutionResult.Create(action);
            }
        }

        public static (float finalDamage, ImmutableArray<ModifierData> consumedModifiers, ImmutableArray<ModifierData> remainingModifiers) ApplyDamageModifiers(
            DamageActionResource damageAction,
            EncounterState encounterState)
        {
            CommonValidation.ThrowIfNull(damageAction, nameof(damageAction));
            CommonValidation.ThrowIfNull(encounterState, nameof(encounterState));

            var activeModifiers = encounterState.Spell.ActiveModifiers;
            var baseDamage = GetRawDamage(damageAction, encounterState);

            _logger.LogInfo($"ApplyDamageModifiers: {damageAction.DamageType} damage {damageAction.Amount} with {activeModifiers.Length} modifiers");

            var modifiedDamage = baseDamage;
            var modifiersToRemove = ImmutableArray.CreateBuilder<ModifierData>();

            foreach (var modifier in activeModifiers)
            {
                if (modifier.CanApply(damageAction.DamageType))
                {
                    var oldDamage = modifiedDamage;
                    modifiedDamage = modifier.Apply(modifiedDamage);
                    _logger.LogInfo($"Applied modifier {modifier.Type} {modifier.Value}: {oldDamage} -> {modifiedDamage}");
                    
                    if (modifier.IsConsumedOnUse)
                    {
                        modifiersToRemove.Add(modifier);
                        _logger.LogInfo($"Marked consumable modifier for removal: {modifier.Type} {modifier.Value}");
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

            var consumedModifiers = modifiersToRemove.ToImmutable();
            _logger.LogInfo($"Final damage: {modifiedDamage}, Consumed {consumedModifiers.Length} modifiers, {remainingModifiers.Length} remaining");
            return (modifiedDamage, consumedModifiers, remainingModifiers);
        }

        public static SpellState AddModifier(SpellState currentState, ModifierData modifier)
        {
            if (currentState == null)
                throw new ArgumentNullException(nameof(currentState));
            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            _logger.LogInfo($"[Spell Casting Refactor] AddModifier: Adding {modifier.Type} {modifier.Value} {modifier.Element} modifier");
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

            _logger.LogInfo($"[Spell Casting Refactor] UpdateProperty: {key} {operation} {floatValue} ({currentValue} -> {newValue})");
            return currentState.WithProperty(key, Variant.From(newValue));
        }
        public static SpellState UpdateProperty(
            SpellState currentState,
            ContextProperty property,
            float value,
            ContextPropertyOperation operation)
        {
            return UpdateProperty(currentState, property.ToString(), Variant.From(value), operation);
        }

        [Obsolete("Use EncounterState snapshot-based execution instead via ApplyEncounterSnapshot")]
        public static SpellState ProcessDamageAction(
            IGameStateData gameState,
            DamageActionResource damageAction)
        {
            throw new InvalidOperationException(
                "ProcessDamageAction is deprecated. Use EncounterState snapshot-based execution instead. " +
                "Ensure PreCalculateSpellCommand creates snapshots and ExecuteCardActionCommand uses ApplyEncounterSnapshot.");
        }

        private static float GetRawDamage(DamageActionResource damageAction, EncounterState encounterState)
            => GetRawDamageInternal(damageAction, encounterState.StatusEffects);

        private static float GetRawDamageInternal(DamageActionResource damageAction, StatusEffectsState statusEffects)
        {
            return damageAction.DamageType switch
            {
                DamageType.None => damageAction.Amount,
                DamageType.Fire => damageAction.Amount,
                DamageType.Frost => damageAction.Amount,
                DamageType.PerChill => damageAction.Amount * StatusEffectLogicManager.GetStacksOfEffect(statusEffects, StatusEffectType.Chill),
                _ => damageAction.Amount
            };
        }

        private static EncounterState SimulateActionEffectsOnEncounterState(
            EncounterState currentEncounterState,
            ActionResource action,
            ActionExecutionResult actionResult)
        {
            switch (action)
            {
                case DamageActionResource damageAction:
                    // Get the correct remaining modifiers directly from ApplyDamageModifiers
                    var (_, _, remainingModifiers) = ApplyDamageModifiers(damageAction, currentEncounterState);
                    
                    // Update spell state with remaining modifiers and context properties
                    var currentSpellState = currentEncounterState.Spell;
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