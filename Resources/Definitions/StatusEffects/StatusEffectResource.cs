using Godot;
using Maximagus.Scripts.Enums;
using Scripts.State;

namespace Maximagus.Resources.Definitions.StatusEffects
{
    /// <summary>
    /// Resource definition for status effects that integrates with the centralized state system
    /// </summary>
    [GlobalClass]
    public partial class StatusEffectResource : Resource
    {
        [Export] public StatusEffectType EffectType { get; set; }
        [Export] public string EffectName { get; set; }
        [Export] public string Description { get; set; }
        [Export] public StatusEffectTrigger Trigger { get; set; }
        [Export] public StatusEffectDecayMode DecayMode { get; set; }
        [Export] public int InitialStacks { get; set; } = 0;
        [Export] public float Value { get; set; }
        [Export] public int MaxStacks { get; set; } = 99;

        /// <summary>
        /// Calculates the effect value for the given number of stacks
        /// This method is pure and doesn't modify any state
        /// </summary>
        /// <param name="stacks">Number of stacks to calculate for</param>
        /// <returns>The calculated effect value</returns>
        public virtual float CalculateEffectValue(int stacks)
        {
            return Value * stacks;
        }

        /// <summary>
        /// Gets the display text for this status effect with the given stacks
        /// </summary>
        /// <param name="stacks">Number of stacks</param>
        /// <returns>Display text for the effect</returns>
        public virtual string GetDisplayText(int stacks)
        {
            var effectValue = CalculateEffectValue(stacks);
            return EffectType switch
            {
                StatusEffectType.Poison => $"Poison: {effectValue} damage ({stacks} stacks)",
                StatusEffectType.Bleeding => $"Bleeding: +{effectValue} damage to attacks ({stacks} stacks)",
                StatusEffectType.Chill => $"Chill: -{effectValue} damage reduction ({stacks} stacks)",
                StatusEffectType.Burning => $"Burning: {effectValue} damage over time ({stacks} stacks)",
                _ => $"{EffectName}: {effectValue} ({stacks} stacks)"
            };
        }

        /// <summary>
        /// Triggers the status effect for logging/display purposes
        /// This method is called by StatusEffectLogicManager during effect processing
        /// </summary>
        /// <param name="stacks">Number of stacks triggering</param>
        public virtual void OnTrigger(int stacks)
        {
            var effectValue = CalculateEffectValue(stacks);
            var displayText = GetDisplayText(stacks);
            
            // Log the effect trigger for debugging/display purposes
            GD.Print($"Status Effect Triggered: {displayText}");
        }

        /// <summary>
        /// Validates that this status effect resource has valid configuration
        /// </summary>
        /// <returns>True if the resource is valid, false otherwise</returns>
        public virtual bool IsValid()
        {
            try
            {
                // Effect name should not be empty
                if (string.IsNullOrWhiteSpace(EffectName))
                    return false;

                // Initial stacks should be positive
                if (InitialStacks <= 0)
                    return false;

                // Max stacks should be greater than or equal to initial stacks
                if (MaxStacks < InitialStacks)
                    return false;

                // Value should be non-negative for most effects
                if (Value < 0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a StatusEffectInstanceData from this resource
        /// </summary>
        /// <param name="stacks">Number of stacks (uses InitialStacks if not specified)</param>
        /// <param name="appliedAt">When the effect was applied (uses current time if not specified)</param>
        /// <returns>A new StatusEffectInstanceData</returns>
        public StatusEffectInstanceData CreateInstance(int stacks = -1, System.DateTime? appliedAt = null)
        {
            return StatusEffectInstanceData.FromResource(this, stacks, appliedAt);
        }

        public override string ToString()
        {
            return $"StatusEffectResource[{EffectType}: {EffectName}, Value: {Value}, Stacks: {InitialStacks}-{MaxStacks}]";
        }
    }
}
