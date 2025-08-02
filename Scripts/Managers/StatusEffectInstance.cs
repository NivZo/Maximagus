using Godot;
using Maximagus.Resources.Definitions.StatusEffects;

namespace Maximagus.Scripts.StatusEffects
{
    public partial class StatusEffectInstance : RefCounted
    {
        public StatusEffectResource Effect { get; set; }
        public int CurrentStacks { get; set; }
        public string InstanceId { get; set; }

        public StatusEffectInstance(StatusEffectResource effect, int stacks = 1)
        {
            Effect = effect;
            CurrentStacks = stacks;
            InstanceId = System.Guid.NewGuid().ToString();
        }

        public void AddStacks(int amount)
        {
            if (Effect.IsStackable)
            {
                CurrentStacks = Mathf.Min(CurrentStacks + amount, Effect.MaxStacks);
            }
        }

        public void ReduceStacks(int amount = 1)
        {
            CurrentStacks = Mathf.Max(0, CurrentStacks - amount);
        }

        public bool IsExpired => CurrentStacks <= 0;
    }
}
