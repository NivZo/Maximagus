using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.StatusEffects;
using Scripts.Commands;
using Scripts.Commands.Spell;

namespace Maximagus.Scripts.Managers
{
    public partial class StatusEffectManager : IStatusEffectManager
    {
        private readonly IGameCommandProcessor _commandProcessor;

        public StatusEffectManager()
        {
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        }

        public void AddStatusEffect(StatusEffectResource effect, int stacks = 1, StatusEffectActionType actionType = StatusEffectActionType.Add)
        {
            var command = new ApplyStatusEffectCommand(effect, stacks, actionType);
            _commandProcessor.ExecuteCommand(command);
        }

        public void TriggerEffects(StatusEffectTrigger trigger)
        {
            var command = new TriggerStatusEffectsCommand(trigger);
            _commandProcessor.ExecuteCommand(command);
        }

        public int GetStacksOfEffect(StatusEffectType statusEffectType)
        {
            return _commandProcessor.CurrentState.StatusEffects.GetStacksOfEffect(statusEffectType);
        }
    }
}
