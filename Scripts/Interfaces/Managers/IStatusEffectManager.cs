using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Enums;

public interface IStatusEffectManager
{
    void AddStatusEffect(StatusEffectResource effect, int stacks = 1, StatusEffectActionType actionType = StatusEffectActionType.Add);
    void TriggerEffects(StatusEffectTrigger trigger);
    int GetStacksOfEffect(StatusEffectType statusEffectType);
}