using System.Collections.Immutable;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.StatusEffects;
using Scripts.State;
using Scripts.Utils;

namespace Maximagus.Scripts.Managers
{

    public static class StatusEffectLogicManager
    {
        // Encounter state convenience methods - callers should extract StatusEffects and use core methods below
        public static EncounterState ApplyStatusEffectToEncounter(
            EncounterState currentEncounterState,
            StatusEffectResource effect,
            int stacks,
            StatusEffectActionType actionType)
        {
            CommonValidation.ThrowIfNull(currentEncounterState, nameof(currentEncounterState));
            CommonValidation.ThrowIfNull(effect, nameof(effect));

            var newStatusEffectsState = ApplyStatusEffect(
                currentEncounterState.StatusEffects,
                effect,
                stacks,
                actionType);

            return currentEncounterState.WithStatusEffects(newStatusEffectsState);
        }

        public static StatusEffectsState ApplyStatusEffect(
            StatusEffectsState currentState,
            StatusEffectResource effect,
            int stacks,
            StatusEffectActionType actionType)
        {
            CommonValidation.ThrowIfNull(currentState, nameof(currentState));
            CommonValidation.ThrowIfNull(effect, nameof(effect));
            return currentState.WithAppliedEffect(effect, stacks, actionType);
        }
        public static StatusEffectsState TriggerEffects(
            StatusEffectsState currentState,
            StatusEffectTrigger trigger)
        {
            CommonValidation.ThrowIfNull(currentState, nameof(currentState));

            // Get all effects that should trigger
            var effectsToTrigger = currentState.GetEffectsForTrigger(trigger);
            
            // Process each triggering effect
            var updatedState = currentState;
            foreach (var effectInstance in effectsToTrigger)
            {
                // Execute the effect's OnTrigger method
                effectInstance.EffectResource.OnTrigger(effectInstance.CurrentStacks);

                // Apply decay based on the effect's decay mode
                updatedState = effectInstance.EffectResource.DecayMode switch
                {
                    StatusEffectDecayMode.ReduceByOneOnTrigger => updatedState.WithAppliedEffect(
                        effectInstance.EffectResource, 1, StatusEffectActionType.Remove),
                    StatusEffectDecayMode.RemoveOnTrigger => updatedState.WithRemovedEffect(effectInstance.EffectType),
                    _ => updatedState
                };
            }

            // Remove any expired effects
            return updatedState.WithExpiredEffectsRemoved();
        }

        public static StatusEffectsState ProcessDecay(
            StatusEffectsState currentState,
            StatusEffectDecayMode decayMode)
        {
            CommonValidation.ThrowIfNull(currentState, nameof(currentState));
            return currentState.WithDecayProcessed(decayMode);
        }
        // Simple delegation methods - these just wrap the state methods
        public static int GetStacksOfEffect(StatusEffectsState currentState, StatusEffectType effectType)
            => currentState?.GetStacksOfEffect(effectType) ?? 0;

        public static bool HasEffect(StatusEffectsState currentState, StatusEffectType effectType)
            => currentState?.HasEffect(effectType) ?? false;

        public static ImmutableArray<StatusEffectInstanceData> GetEffectsForTrigger(StatusEffectsState currentState, StatusEffectTrigger trigger)
            => currentState?.GetEffectsForTrigger(trigger) ?? ImmutableArray<StatusEffectInstanceData>.Empty;

        public static StatusEffectInstanceData GetEffect(StatusEffectsState currentState, StatusEffectType effectType)
            => currentState?.GetEffect(effectType);

        public static StatusEffectsState RemoveExpiredEffects(StatusEffectsState currentState)
            => currentState?.WithExpiredEffectsRemoved();

        public static bool ValidateState(StatusEffectsState currentState)
            => currentState?.IsValid() ?? false;
    }
}