using System;
using Scripts.State;
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Spell
{
    /// <summary>
    /// Command to trigger all status effects that match a specific trigger type.
    /// Processes status effect triggers and applies any resulting decay through StatusEffectLogicManager.
    /// </summary>
    public class TriggerStatusEffectsCommand : GameCommand
    {
        private readonly StatusEffectTrigger _trigger;

        public TriggerStatusEffectsCommand(StatusEffectTrigger trigger) : base(false)
        {
            _trigger = trigger;
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState == null) 
            {
                _logger.LogWarning("[TriggerStatusEffectsCommand] Cannot execute - no current state");
                return false;
            }

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            
            _logger.LogInfo($"[TriggerStatusEffectsCommand] Triggering status effects for: {_trigger}");
            
            try
            {
                // Get effects that will trigger before processing
                var effectsToTrigger = currentState.StatusEffects.GetEffectsForTrigger(_trigger);
                
                if (effectsToTrigger.Length == 0)
                {
                    _logger.LogInfo($"[TriggerStatusEffectsCommand] No effects to trigger for {_trigger}");
                    token.Complete(CommandResult.Success(currentState));
                    return;
                }

                _logger.LogInfo($"[TriggerStatusEffectsCommand] Found {effectsToTrigger.Length} effects to trigger");

                // Use StatusEffectLogicManager to trigger effects
                var newStatusEffectsState = StatusEffectLogicManager.TriggerEffects(
                    currentState.StatusEffects,
                    _trigger);

                var newState = currentState.WithStatusEffects(newStatusEffectsState);

                _logger.LogInfo($"[TriggerStatusEffectsCommand] Successfully triggered status effects for {_trigger}");
                
                token.Complete(CommandResult.Success(newState));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[TriggerStatusEffectsCommand] Error triggering status effects: {ex.Message}");
                token.Complete(CommandResult.Failure($"Failed to trigger status effects: {ex.Message}"));
            }
        }

        public override string GetDescription()
        {
            return $"Trigger status effects for {_trigger}";
        }

        /// <summary>
        /// Gets the trigger type for testing purposes
        /// </summary>
        public StatusEffectTrigger GetTrigger()
        {
            return _trigger;
        }
    }
}