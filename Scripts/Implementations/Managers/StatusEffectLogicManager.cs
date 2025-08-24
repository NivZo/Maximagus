using System.Collections.Immutable;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.StatusEffects;
using Scripts.State;

namespace Maximagus.Scripts.Managers
{
    /// <summary>
    /// Static manager class for status effect business logic operations
    /// Provides pure functions that operate on StatusEffectsState without side effects
    /// </summary>
    public static class StatusEffectLogicManager
    {
        /// <summary>
        /// Applies a status effect to an EncounterState, returning a new EncounterState with updated status effects.
        /// This method works seamlessly with EncounterState-based calculations.
        /// </summary>
        /// <param name="currentEncounterState">The current encounter state</param>
        /// <param name="effect">The status effect resource to apply</param>
        /// <param name="stacks">Number of stacks to apply</param>
        /// <param name="actionType">How to apply the stacks (Add, Set, Remove)</param>
        /// <returns>New EncounterState with the effect applied</returns>
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

        /// <summary>
        /// Triggers all status effects that match the given trigger type in an EncounterState.
        /// Returns a new EncounterState with effects triggered and decay applied.
        /// </summary>
        /// <param name="currentEncounterState">The current encounter state</param>
        /// <param name="trigger">The trigger type to process</param>
        /// <returns>New EncounterState after processing triggers and applying decay</returns>
        public static EncounterState TriggerEffectsInEncounter(
            EncounterState currentEncounterState,
            StatusEffectTrigger trigger)
        {
            if (currentEncounterState == null)
                throw new System.ArgumentNullException(nameof(currentEncounterState));

            var newStatusEffectsState = TriggerEffects(currentEncounterState.StatusEffects, trigger);
            return currentEncounterState.WithStatusEffects(newStatusEffectsState);
        }

        /// <summary>
        /// Processes status effect decay for an EncounterState.
        /// Returns a new EncounterState with decay processed.
        /// </summary>
        /// <param name="currentEncounterState">The current encounter state</param>
        /// <param name="decayMode">The decay mode to process</param>
        /// <returns>New EncounterState after processing decay</returns>
        public static EncounterState ProcessDecayInEncounter(
            EncounterState currentEncounterState,
            StatusEffectDecayMode decayMode)
        {
            if (currentEncounterState == null)
                throw new System.ArgumentNullException(nameof(currentEncounterState));

            var newStatusEffectsState = ProcessDecay(currentEncounterState.StatusEffects, decayMode);
            return currentEncounterState.WithStatusEffects(newStatusEffectsState);
        }
        /// <summary>
        /// Applies a status effect to the current state, handling stacking logic
        /// </summary>
        /// <param name="currentState">The current status effects state</param>
        /// <param name="effect">The status effect resource to apply</param>
        /// <param name="stacks">Number of stacks to apply</param>
        /// <param name="actionType">How to apply the stacks (Add, Set, Remove)</param>
        /// <returns>New state with the effect applied</returns>
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

        /// <summary>
        /// Triggers all status effects that match the given trigger type
        /// </summary>
        /// <param name="currentState">The current status effects state</param>
        /// <param name="trigger">The trigger type to process</param>
        /// <returns>New state after processing triggers and applying decay</returns>
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

        /// <summary>
        /// Processes status effect decay for end-of-turn or other decay modes
        /// </summary>
        /// <param name="currentState">The current status effects state</param>
        /// <param name="decayMode">The decay mode to process</param>
        /// <returns>New state after processing decay</returns>
        public static StatusEffectsState ProcessDecay(
            StatusEffectsState currentState,
            StatusEffectDecayMode decayMode)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            // Use the existing WithDecayProcessed method
            return currentState.WithDecayProcessed(decayMode);
        }

        /// <summary>
        /// Gets the total number of stacks for a specific effect type
        /// </summary>
        /// <param name="currentState">The current status effects state</param>
        /// <param name="effectType">The effect type to query</param>
        /// <returns>Total stacks of the specified effect type</returns>
        public static int GetStacksOfEffect(
            StatusEffectsState currentState,
            StatusEffectType effectType)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            return currentState.GetStacksOfEffect(effectType);
        }

        /// <summary>
        /// Checks if a specific effect type is currently active
        /// </summary>
        /// <param name="currentState">The current status effects state</param>
        /// <param name="effectType">The effect type to check</param>
        /// <returns>True if the effect is active, false otherwise</returns>
        public static bool HasEffect(
            StatusEffectsState currentState,
            StatusEffectType effectType)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            return currentState.HasEffect(effectType);
        }

        /// <summary>
        /// Gets all active effects that match a specific trigger
        /// </summary>
        /// <param name="currentState">The current status effects state</param>
        /// <param name="trigger">The trigger type to filter by</param>
        /// <returns>Array of effects that match the trigger</returns>
        public static ImmutableArray<StatusEffectInstanceData> GetEffectsForTrigger(
            StatusEffectsState currentState,
            StatusEffectTrigger trigger)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            return currentState.GetEffectsForTrigger(trigger);
        }

        /// <summary>
        /// Gets a specific effect instance by type (returns first if multiple exist)
        /// </summary>
        /// <param name="currentState">The current status effects state</param>
        /// <param name="effectType">The effect type to get</param>
        /// <returns>The effect instance, or null if not found</returns>
        public static StatusEffectInstanceData GetEffect(
            StatusEffectsState currentState,
            StatusEffectType effectType)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            return currentState.GetEffect(effectType);
        }

        /// <summary>
        /// Removes all expired effects from the state
        /// </summary>
        /// <param name="currentState">The current status effects state</param>
        /// <returns>New state with expired effects removed</returns>
        public static StatusEffectsState RemoveExpiredEffects(StatusEffectsState currentState)
        {
            if (currentState == null)
                throw new System.ArgumentNullException(nameof(currentState));

            return currentState.WithExpiredEffectsRemoved();
        }

        /// <summary>
        /// Validates that the status effects state is consistent and valid
        /// </summary>
        /// <param name="currentState">The status effects state to validate</param>
        /// <returns>True if the state is valid, false otherwise</returns>
        public static bool ValidateState(StatusEffectsState currentState)
        {
            if (currentState == null)
                return false;

            return currentState.IsValid();
        }
    }
}