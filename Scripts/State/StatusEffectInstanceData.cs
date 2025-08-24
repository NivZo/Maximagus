using System;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.StatusEffects;

namespace Scripts.State
{
    /// <summary>
    /// Immutable data representing a status effect instance in state
    /// </summary>
    public partial class StatusEffectInstanceData
    {
        public StatusEffectType EffectType { get; }
        public int CurrentStacks { get; }
        public StatusEffectResource EffectResource { get; }
        public DateTime AppliedAt { get; }

        public StatusEffectInstanceData(
            StatusEffectType effectType,
            int currentStacks,
            StatusEffectResource effectResource,
            DateTime appliedAt)
        {
            EffectType = effectType;
            CurrentStacks = currentStacks;
            EffectResource = effectResource ?? throw new ArgumentNullException(nameof(effectResource));
            AppliedAt = appliedAt;
        }

        /// <summary>
        /// Creates a StatusEffectInstanceData from a StatusEffectResource
        /// </summary>
        public static StatusEffectInstanceData FromResource(StatusEffectResource resource, int stacks = -1, DateTime? appliedAt = null)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            var actualStacks = stacks > 0 ? stacks : resource.InitialStacks;
            var actualAppliedAt = appliedAt ?? DateTime.UtcNow;

            return new StatusEffectInstanceData(
                resource.EffectType,
                actualStacks,
                resource,
                actualAppliedAt);
        }

        /// <summary>
        /// Creates a test StatusEffectInstanceData with minimal resource for testing purposes
        /// </summary>
        public static StatusEffectInstanceData Create(StatusEffectType effectType, int stacks, DateTime? appliedAt = null)
        {
            // Create a minimal test resource
            var testResource = new TestStatusEffectResource(effectType);
            var actualAppliedAt = appliedAt ?? DateTime.UtcNow;

            return new StatusEffectInstanceData(
                effectType,
                stacks,
                testResource,
                actualAppliedAt);
        }

        /// <summary>
        /// Minimal test implementation of StatusEffectResource for testing
        /// </summary>
        private partial class TestStatusEffectResource : Maximagus.Resources.Definitions.StatusEffects.StatusEffectResource
        {
            public TestStatusEffectResource()
            {
                // Default constructor for Godot
            }

            public TestStatusEffectResource(StatusEffectType effectType)
            {
                EffectType = effectType;
                EffectName = effectType.ToString();
                Description = $"Test {effectType} effect";
                InitialStacks = 1;
                MaxStacks = 10;
                Value = 1.0f;
                DecayMode = StatusEffectDecayMode.EndOfTurn;
                Trigger = StatusEffectTrigger.OnDamageDealt;
            }
        }

        /// <summary>
        /// Creates a new instance with updated stack count
        /// </summary>
        public StatusEffectInstanceData WithStacks(int newStacks)
        {
            return new StatusEffectInstanceData(
                EffectType,
                Math.Max(0, Math.Min(newStacks, EffectResource.MaxStacks)),
                EffectResource,
                AppliedAt);
        }

        /// <summary>
        /// Creates a new instance with added stacks
        /// </summary>
        public StatusEffectInstanceData WithAddedStacks(int stacksToAdd)
        {
            return WithStacks(CurrentStacks + stacksToAdd);
        }

        /// <summary>
        /// Creates a new instance with reduced stacks
        /// </summary>
        public StatusEffectInstanceData WithReducedStacks(int stacksToReduce)
        {
            return WithStacks(CurrentStacks - stacksToReduce);
        }

        /// <summary>
        /// Determines if this status effect instance has expired (no stacks remaining)
        /// </summary>
        public bool IsExpired => CurrentStacks <= 0;

        /// <summary>
        /// Determines if this status effect should decay based on the decay mode
        /// </summary>
        public bool ShouldDecay(StatusEffectDecayMode decayMode)
        {
            return EffectResource.DecayMode == decayMode;
        }

        /// <summary>
        /// Determines if this status effect should trigger for the given trigger type
        /// </summary>
        public bool ShouldTrigger(StatusEffectTrigger trigger)
        {
            return EffectResource.Trigger == trigger;
        }

        /// <summary>
        /// Validates that the status effect instance data is consistent
        /// </summary>
        public bool IsValid()
        {
            try
            {
                // Effect resource must not be null
                if (EffectResource == null)
                    return false;

                // Effect type must match resource
                if (EffectType != EffectResource.EffectType)
                    return false;

                // Current stacks should be within valid range
                if (CurrentStacks < 0 || CurrentStacks > EffectResource.MaxStacks)
                    return false;

                // Applied time should be reasonable (not in the future by more than a few seconds)
                if (AppliedAt > DateTime.UtcNow.AddSeconds(5))
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
            if (obj is StatusEffectInstanceData other)
            {
                return EffectType == other.EffectType &&
                       CurrentStacks == other.CurrentStacks &&
                       ReferenceEquals(EffectResource, other.EffectResource) &&
                       AppliedAt == other.AppliedAt;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EffectType, CurrentStacks, EffectResource, AppliedAt);
        }

        public override string ToString()
        {
            return $"StatusEffectInstanceData[{EffectType} x{CurrentStacks}, Applied: {AppliedAt:HH:mm:ss}]";
        }
    }
}