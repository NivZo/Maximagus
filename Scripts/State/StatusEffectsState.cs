using System;
using System.Collections.Immutable;
using System.Linq;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.StatusEffects;

namespace Scripts.State
{
    /// <summary>
    /// Immutable state representing all active status effects
    /// </summary>
    public class StatusEffectsState
    {
        public ImmutableArray<StatusEffectInstanceData> ActiveEffects { get; }

        public StatusEffectsState(ImmutableArray<StatusEffectInstanceData> activeEffects = default)
        {
            ActiveEffects = activeEffects.IsDefault ? ImmutableArray<StatusEffectInstanceData>.Empty : activeEffects;
        }

        /// <summary>
        /// Creates an initial empty status effects state
        /// </summary>
        public static StatusEffectsState CreateInitial()
        {
            return new StatusEffectsState();
        }

        /// <summary>
        /// Creates a new state with updated active effects
        /// </summary>
        public StatusEffectsState WithActiveEffects(ImmutableArray<StatusEffectInstanceData> newActiveEffects)
        {
            return new StatusEffectsState(newActiveEffects);
        }

        /// <summary>
        /// Creates a new state with an added status effect
        /// </summary>
        public StatusEffectsState WithAddedEffect(StatusEffectInstanceData effect)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));

            return new StatusEffectsState(ActiveEffects.Add(effect));
        }

        /// <summary>
        /// Creates a new state with a status effect applied (stacking if it already exists)
        /// </summary>
        public StatusEffectsState WithAppliedEffect(StatusEffectResource effectResource, int stacks, StatusEffectActionType actionType)
        {
            if (effectResource == null)
                throw new ArgumentNullException(nameof(effectResource));

            var existingEffect = ActiveEffects.FirstOrDefault(e => e.EffectType == effectResource.EffectType);
            
            if (existingEffect != null)
            {
                // Update existing effect based on action type
                var updatedEffect = actionType switch
                {
                    StatusEffectActionType.Add => existingEffect.WithAddedStacks(stacks),
                    StatusEffectActionType.Set => existingEffect.WithStacks(stacks),
                    StatusEffectActionType.Remove => existingEffect.WithReducedStacks(stacks),
                    _ => existingEffect
                };

                // Remove the old effect and add the updated one (or remove if expired)
                var newActiveEffects = ActiveEffects.Remove(existingEffect);
                if (!updatedEffect.IsExpired)
                {
                    newActiveEffects = newActiveEffects.Add(updatedEffect);
                }

                return new StatusEffectsState(newActiveEffects);
            }
            else if (actionType != StatusEffectActionType.Remove && stacks > 0)
            {
                // Add new effect if not removing
                var newEffect = StatusEffectInstanceData.FromResource(effectResource, stacks);
                return WithAddedEffect(newEffect);
            }

            // No change if trying to remove non-existent effect
            return this;
        }

        /// <summary>
        /// Creates a new state with a specific effect removed
        /// </summary>
        public StatusEffectsState WithRemovedEffect(StatusEffectType effectType)
        {
            var effectToRemove = ActiveEffects.FirstOrDefault(e => e.EffectType == effectType);
            if (effectToRemove != null)
            {
                return new StatusEffectsState(ActiveEffects.Remove(effectToRemove));
            }
            return this;
        }

        /// <summary>
        /// Creates a new state with expired effects removed
        /// </summary>
        public StatusEffectsState WithExpiredEffectsRemoved()
        {
            var nonExpiredEffects = ActiveEffects.Where(e => !e.IsExpired).ToImmutableArray();
            return new StatusEffectsState(nonExpiredEffects);
        }

        /// <summary>
        /// Creates a new state with effects processed for decay
        /// </summary>
        public StatusEffectsState WithDecayProcessed(StatusEffectDecayMode decayMode)
        {
            var updatedEffects = ActiveEffects.Select(effect =>
            {
                if (!effect.ShouldDecay(decayMode))
                    return effect;

                return decayMode switch
                {
                    StatusEffectDecayMode.RemoveOnTrigger => null, // Will be filtered out
                    StatusEffectDecayMode.ReduceByOneOnTrigger => effect.WithReducedStacks(1),
                    StatusEffectDecayMode.EndOfTurn => effect.WithReducedStacks(effect.CurrentStacks),
                    StatusEffectDecayMode.ReduceByOneEndOfTurn => effect.WithReducedStacks(1),
                    _ => effect
                };
            })
            .Where(effect => effect != null && !effect.IsExpired)
            .ToImmutableArray();

            return new StatusEffectsState(updatedEffects);
        }

        /// <summary>
        /// Gets all effects that should trigger for the given trigger type
        /// </summary>
        public ImmutableArray<StatusEffectInstanceData> GetEffectsForTrigger(StatusEffectTrigger trigger)
        {
            return ActiveEffects.Where(e => e.ShouldTrigger(trigger)).ToImmutableArray();
        }

        /// <summary>
        /// Gets the total stacks of a specific effect type
        /// </summary>
        public int GetStacksOfEffect(StatusEffectType effectType)
        {
            return ActiveEffects
                .Where(e => e.EffectType == effectType)
                .Sum(e => e.CurrentStacks);
        }

        /// <summary>
        /// Gets a specific effect instance by type (returns first if multiple exist)
        /// </summary>
        public StatusEffectInstanceData GetEffect(StatusEffectType effectType)
        {
            return ActiveEffects.FirstOrDefault(e => e.EffectType == effectType);
        }

        /// <summary>
        /// Determines if any effects of the specified type are active
        /// </summary>
        public bool HasEffect(StatusEffectType effectType)
        {
            return ActiveEffects.Any(e => e.EffectType == effectType);
        }

        /// <summary>
        /// Gets the total number of active status effects
        /// </summary>
        public int TotalActiveEffects => ActiveEffects.Length;

        /// <summary>
        /// Determines if there are any active status effects
        /// </summary>
        public bool HasAnyActiveEffects => ActiveEffects.Length > 0;

        /// <summary>
        /// Validates that the status effects state is consistent
        /// </summary>
        public bool IsValid()
        {
            try
            {
                // Validate all effect instances
                foreach (var effect in ActiveEffects)
                {
                    if (!effect.IsValid())
                        return false;
                }

                // Check for duplicate effect types (should not happen in a well-formed state)
                var effectTypes = ActiveEffects.Select(e => e.EffectType).ToArray();
                if (effectTypes.Length != effectTypes.Distinct().Count())
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is StatusEffectsState other)
            {
                return ActiveEffects.SequenceEqual(other.ActiveEffects);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ActiveEffects.Length);
        }

        public override string ToString()
        {
            if (ActiveEffects.IsEmpty)
                return "StatusEffectsState[No active effects]";

            var effectsSummary = string.Join(", ", ActiveEffects.Select(e => $"{e.EffectType}x{e.CurrentStacks}"));
            return $"StatusEffectsState[{effectsSummary}]";
        }
    }
}