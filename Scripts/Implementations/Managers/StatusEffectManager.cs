using Godot;
using Godot.Collections;
using System.Linq;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.StatusEffects;
using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Events;

namespace Maximagus.Scripts.Managers
{
    public partial class StatusEffectManager : IStatusEffectManager
    {
        private Array<StatusEffectInstance> _activeEffects = new();

        public void AddStatusEffect(StatusEffectResource effect, int stacks = 1, StatusEffectActionType actionType = StatusEffectActionType.Add)
        {
            var existingEffect = _activeEffects.FirstOrDefault(e => e.Effect.EffectType == effect.EffectType);

            var newStacks = (existingEffect != null, actionType) switch
            {
                (true, StatusEffectActionType.Add) => existingEffect.CurrentStacks + stacks,
                (false, StatusEffectActionType.Add) => stacks,
                (true, StatusEffectActionType.Multiply) => existingEffect.CurrentStacks * stacks,
                (false, StatusEffectActionType.Multiply) => 0,
                (_, StatusEffectActionType.Set) => stacks,
                _ => 0,
            };


            if (existingEffect != null)
            {
                existingEffect.SetStacks(newStacks);
            }
            else
            {
                var newInstance = new StatusEffectInstance(effect, newStacks);
                _activeEffects.Add(newInstance);
            }
        }

        public void TriggerEffects(StatusEffectTrigger trigger)
        {
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
                        effectInstance.SetStacks(0);
                    }
                }
            }

            RemoveExpiredEffects();
        }

        private void RemoveExpiredEffects()
        {
            foreach (var effect in _activeEffects)
            {
                if (effect.IsExpired)
                {
                    _activeEffects.Remove(effect);
                }
            }
        }

        public void ProcessEndOfTurnDecay()
        {
            foreach (var effectInstance in _activeEffects)
            {
                switch (effectInstance.Effect.DecayMode)
                {
                    case StatusEffectDecayMode.EndOfTurn:
                        effectInstance.SetStacks(0);
                        break;
                    case StatusEffectDecayMode.ReduceByOneEndOfTurn:
                        effectInstance.ReduceStacks();
                        break;
                }
            }

            RemoveExpiredEffects();
        }

        public int GetStacksOfEffect(StatusEffectType effectType)
        {
            return _activeEffects
                .Where(e => e.Effect.EffectType == effectType)
                .Sum(e => e.CurrentStacks);
        }

        private void ClearActiveEffects() => _activeEffects.Clear();

        private Array<StatusEffectInstance> GetActiveEffects() => _activeEffects;
    }
}
