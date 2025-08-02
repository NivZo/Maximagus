using Godot;
using Godot.Collections;
using System.Linq;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.StatusEffects;
using Maximagus.Resources.Definitions.StatusEffects;

namespace Maximagus.Scripts.Managers
{
    public partial class StatusEffectManager : IStatusEffectManager
    {
        private Array<StatusEffectInstance> _activeEffects = new();

        public void AddStatusEffect(StatusEffectResource effect, int stacks = 1)
        {
            var existingEffect = _activeEffects.FirstOrDefault(e => 
                e.Effect.EffectType == effect.EffectType && e.Effect.IsStackable);

            if (existingEffect != null && effect.IsStackable)
            {
                existingEffect.AddStacks(stacks);
                GD.Print($"Added {stacks} stacks to {effect.EffectName}. Total: {existingEffect.CurrentStacks}");
            }
            else
            {
                var newInstance = new StatusEffectInstance(effect, stacks);
                _activeEffects.Add(newInstance);
                GD.Print($"Applied new status effect: {effect.EffectName} with {stacks} stacks");
            }
        }

        public void TriggerEffects(StatusEffectTrigger trigger)
        {
            var effectsToRemove = new Array<StatusEffectInstance>();

            foreach (var effectInstance in _activeEffects)
            {
                GD.Print($"Triggerring status effect {trigger}");
                if (effectInstance.Effect.Trigger == trigger)
                {
                    effectInstance.Effect.OnTrigger(effectInstance.CurrentStacks);

                    if (effectInstance.Effect.DecayMode == StatusEffectDecayMode.ReduceByOneOnTrigger)
                    {
                        effectInstance.ReduceStacks();
                    }
                    else if (effectInstance.Effect.DecayMode == StatusEffectDecayMode.RemoveOnTrigger)
                    {
                        effectInstance.ReduceStacks(GetStacksOfEffect(effectInstance.Effect.EffectType));
                    }

                    if (effectInstance.IsExpired)
                    {
                        effectsToRemove.Add(effectInstance);
                    }
                }
            }

            // Remove expired effects
            foreach (var expiredEffect in effectsToRemove)
            {
                _activeEffects.Remove(expiredEffect);
                GD.Print($"Removed expired status effect: {expiredEffect.Effect.EffectName}");
            }
        }

        public void ProcessEndOfTurnDecay()
        {
            var effectsToRemove = new Array<StatusEffectInstance>();

            foreach (var effectInstance in _activeEffects)
            {
                switch (effectInstance.Effect.DecayMode)
                {
                    case StatusEffectDecayMode.EndOfTurn:
                        effectsToRemove.Add(effectInstance);
                        break;
                    case StatusEffectDecayMode.ReduceByOneEndOfTurn:
                        effectInstance.ReduceStacks();
                        if (effectInstance.IsExpired)
                            effectsToRemove.Add(effectInstance);
                        break;
                }
            }

            foreach (var expiredEffect in effectsToRemove)
            {
                _activeEffects.Remove(expiredEffect);
                GD.Print($"End of turn removed: {expiredEffect.Effect.EffectName}");
            }
        }

        private void ClearActiveEffects() => _activeEffects.Clear();

        private Array<StatusEffectInstance> GetActiveEffects() => _activeEffects;
        
        private int GetStacksOfEffect(StatusEffectType effectType)
        {
            return _activeEffects
                .Where(e => e.Effect.EffectType == effectType)
                .Sum(e => e.CurrentStacks);
        }
    }
}
