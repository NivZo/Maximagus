using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Godot;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Services;
using Scripts.State;

namespace Maximagus.Scripts.Managers
{
    public static class SpellLogicManager
    {
        // Snapshot management - delegated to SpellSnapshotService
        public static ImmutableArray<EncounterStateSnapshot> PreCalculateSpellWithSnapshots(
            IGameStateData initialGameState,
            IEnumerable<CardState> playedCards)
        {
            return SpellServiceContainer.SnapshotService.PreCalculateSpellWithSnapshots(initialGameState, playedCards);
        }

        public static IGameStateData ApplyEncounterSnapshot(
            IGameStateData gameState,
            EncounterStateSnapshot snapshot)
        {
            return SpellServiceContainer.SnapshotService.ApplyEncounterSnapshot(gameState, snapshot);
        }

        // Core orchestration method - handles action result calculation
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
                    var (finalDamage, consumedModifiers, remainingModifiers) = SpellServiceContainer.DamageService.ApplyDamageModifiers(damageAction, encounterState);
                    return ActionExecutionResult.Create(damageAction, finalDamage, consumedModifiers, remainingModifiers);

                case ModifierActionResource modifierAction:
                    var currentModifiers = encounterState.Spell.ActiveModifiers;
                    var newModifier = ModifierData.FromActionResource(modifierAction);
                    var modifiersAfterAddition = currentModifiers.Add(newModifier);
                    return ActionExecutionResult.Create(modifierAction, 0, ImmutableArray<ModifierData>.Empty, modifiersAfterAddition);

                default:
                    var unchangedModifiers = encounterState.Spell.ActiveModifiers;
                    return ActionExecutionResult.Create(action, 0, ImmutableArray<ModifierData>.Empty, unchangedModifiers);
            }
        }

        // Spell state management - delegated to SpellStateService
        public static SpellState AddModifier(SpellState currentState, ModifierData modifier)
        {
            return SpellServiceContainer.StateService.AddModifier(currentState, modifier);
        }

        // Property management - delegated to SpellStateService
        public static SpellState UpdateProperty(
            SpellState currentState,
            string key,
            Variant value,
            ContextPropertyOperation operation)
        {
            return SpellServiceContainer.StateService.UpdateProperty(currentState, key, value, operation);
        }

        public static SpellState UpdateProperty(
            SpellState currentState,
            ContextProperty property,
            float value,
            ContextPropertyOperation operation)
        {
            return SpellServiceContainer.StateService.UpdateProperty(currentState, property, value, operation);
        }

        // Damage calculation - delegated to DamageCalculationService
        public static (float finalDamage, ImmutableArray<ModifierData> consumedModifiers, ImmutableArray<ModifierData> remainingModifiers) ApplyDamageModifiers(
            DamageActionResource damageAction,
            EncounterState encounterState)
        {
            return SpellServiceContainer.DamageService.ApplyDamageModifiers(damageAction, encounterState);
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
    }
}