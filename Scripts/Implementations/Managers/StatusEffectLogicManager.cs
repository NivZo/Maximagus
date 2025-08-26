using System.Collections.Immutable;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.StatusEffects;
using Scripts.State;

namespace Maximagus.Scripts.Managers
{

    public static class StatusEffectLogicManager
    {
        public static EncounterState ApplyStatusEffectToEncounter(
            EncounterState currentEncounterState,
            StatusEffectResource effect,
            int stacks,
            StatusEffectActionType actionType)
        {
            if (currentEncounterState == null)
                throw new System.ArgumentNullException(nameof(currentEncounterState));
            if (effect == null)
                throw new System.ArgumentNullException(nameof(effect));

            var newStatusEffectsState = ApplyStatusEffect(
                currentEncounterState.StatusEffects,
                effect,
                stacks,
                actionType);

            return currentEncounterState.WithStatusEffects(newStatusEffectsState);
        }
        public static EncounterState TriggerEffectsInEncounter(
            EncounterState currentEncounterState,
            StatusEffectTrigger trigger)
        {
            if (currentEncounterState == null)
                throw new System.ArgumentNullException(nameof(currentEncounterState));

            var newStatusEffectsState = TriggerEffects(currentEncounterState.StatusEffects, trigger);
            return currentEncounterState.WithStatusEffects(newStatusEffectsState);
        }
        public static EncounterState ProcessDecayInEncounter(
            EncounterState currentEncounterState,
            StatusEffectDecayMode decayMode)
        {
            if (currentEncounterState == null)
                throw new System.ArgumentNullException(nameof(currentEncounterState));

            var newStatusEffectsState = ProcessDecay(currentEncounterState.StatusEffects, decayMode);
            return currentEncounterState.WithStatusEffects(newStatusEffectsState);
        }
        public static StatusEffectsState ApplyStatusEffect(
            StatusEffectsState currentState,
            StatusEffectResource effect,
            int stacks,
            StatusEffectActionType actionType)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));
            if (effect == null)
                throw new System.ArgumentNullException(nameof(effect));

            return currentState.WithAppliedEffect(effect, stacks, actionType);
        }
        public static StatusEffectsState TriggerEffects(
            StatusEffectsState currentState,
            StatusEffectTrigger trigger)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            // Get all effects that should trigger
            var effectsToTrigger = currentState.GetEffectsForTrigger(trigger);
            
            // Process each triggering effect
            var updatedState = currentState;
            foreach (var effectInstance in effectsToTrigger)
            {
                // Execute the effect's OnTrigger method
                effectInstance.EffectResource.OnTrigger(effectInstance.CurrentStacks);

                // Apply decay based on the effect's decay mode
                if (effectInstance.EffectResource.DecayMode == StatusEffectDecayMode.ReduceByOneOnTrigger)
                {
                    updatedState = updatedState.WithAppliedEffect(
                        effectInstance.EffectResource, 
                        1, 
                        StatusEffectActionType.Remove);
                }
                else if (effectInstance.EffectResource.DecayMode == StatusEffectDecayMode.RemoveOnTrigger)
                {
                    updatedState = updatedState.WithRemovedEffect(effectInstance.EffectType);
                }
            }

            // Remove any expired effects
            return updatedState.WithExpiredEffectsRemoved();
        }
        public static StatusEffectsState ProcessDecay(
            StatusEffectsState currentState,
            StatusEffectDecayMode decayMode)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            // Use the existing WithDecayProcessed method
            return currentState.WithDecayProcessed(decayMode);
        }
        public static int GetStacksOfEffect(
            StatusEffectsState currentState,
            StatusEffectType effectType)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            return currentState.GetStacksOfEffect(effectType);
        }
        public static bool HasEffect(
            StatusEffectsState currentState,
            StatusEffectType effectType)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            return currentState.HasEffect(effectType);
        }
        public static ImmutableArray<StatusEffectInstanceData> GetEffectsForTrigger(
            StatusEffectsState currentState,
            StatusEffectTrigger trigger)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            return currentState.GetEffectsForTrigger(trigger);
        }
        public static StatusEffectInstanceData GetEffect(
            StatusEffectsState currentState,
            StatusEffectType effectType)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            return currentState.GetEffect(effectType);
        }
        public static StatusEffectsState RemoveExpiredEffects(StatusEffectsState currentState)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            return currentState.WithExpiredEffectsRemoved();
        }
        public static bool ValidateState(StatusEffectsState currentState)
        {
            if (currentState == null)
                return false;

            return currentState.IsValid();
        }
    }
}