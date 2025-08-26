using Godot;
using Maximagus.Scripts.Enums;
using Scripts.State;

namespace Maximagus.Resources.Definitions.StatusEffects
{

    [GlobalClass]
    public partial class StatusEffectResource : Resource
    {
        private static readonly ILogger _logger = ServiceLocator.GetService<ILogger>();

        [Export] public StatusEffectType EffectType { get; set; }
        [Export] public string EffectName { get; set; }
        [Export] public string Description { get; set; }
        [Export] public StatusEffectTrigger Trigger { get; set; }
        [Export] public StatusEffectDecayMode DecayMode { get; set; }
        [Export] public int InitialStacks { get; set; } = 0;
        [Export] public float Value { get; set; }
        [Export] public int MaxStacks { get; set; } = 99;
        public virtual float CalculateEffectValue(int stacks)
        {
            return Value * stacks;
        }
        public virtual string GetDisplayText(int stacks)
        {
            var effectValue = CalculateEffectValue(stacks);
            return EffectType switch
            {
                StatusEffectType.Chill => $"Chill: -{effectValue} damage reduction ({stacks} stacks)",
                StatusEffectType.Burning => $"Burning: {effectValue} damage over time ({stacks} stacks)",
                _ => $"{EffectName}: {effectValue} ({stacks} stacks)"
            };
        }
        public virtual void OnTrigger(int stacks)
        {
            var effectValue = CalculateEffectValue(stacks);
            var displayText = GetDisplayText(stacks);
            
            // Log the effect trigger for debugging/display purposes
            _logger.LogInfo($"Status Effect Triggered: {displayText}");
        }
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
