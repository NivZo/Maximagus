using System;
using Scripts.State;
using Godot;
using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Spell
{

    public class ApplyStatusEffectCommand : GameCommand
    {
        private readonly StatusEffectResource _statusEffect;
        private readonly int _stacks;
        private readonly StatusEffectActionType _actionType;

        public ApplyStatusEffectCommand(
            StatusEffectResource statusEffect, 
            int stacks, 
            StatusEffectActionType actionType) : base()
        {
            _statusEffect = statusEffect ?? throw new ArgumentNullException(nameof(statusEffect));
            _stacks = stacks;
            _actionType = actionType;
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState == null) 
            {
                _logger.LogWarning("[ApplyStatusEffectCommand] Cannot execute - no current state");
                return false;
            }

            if (_statusEffect == null)
            {
                _logger.LogWarning("[ApplyStatusEffectCommand] Cannot execute - status effect is null");
                return false;
            }

            if (_stacks < 0)
            {
                _logger.LogWarning("[ApplyStatusEffectCommand] Cannot execute - negative stacks not allowed");
                return false;
            }

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            
            _logger.LogInfo($"[ApplyStatusEffectCommand] Applying {_actionType} {_stacks} stacks of {_statusEffect.EffectType}");
            
            try
            {
                // Use StatusEffectLogicManager to apply the effect
                var newStatusEffectsState = StatusEffectLogicManager.ApplyStatusEffect(
                    currentState.StatusEffects,
                    _statusEffect,
                    _stacks,
                    _actionType);

                var newState = currentState.WithStatusEffects(newStatusEffectsState);

                _logger.LogInfo($"[ApplyStatusEffectCommand] Successfully applied status effect. Total stacks: {newStatusEffectsState.GetStacksOfEffect(_statusEffect.EffectType)}");
                
                token.Complete(CommandResult.Success(newState));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ApplyStatusEffectCommand] Error applying status effect: {ex.Message}");
                token.Complete(CommandResult.Failure($"Failed to apply status effect: {ex.Message}"));
            }
        }

        public override string GetDescription()
        {
            return $"Apply {_actionType} {_stacks} stacks of {_statusEffect.EffectType}";
        }
    }
}