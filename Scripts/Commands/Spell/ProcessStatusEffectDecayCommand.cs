using System;
using Scripts.State;
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Spell
{
    /// <summary>
    /// Command to process status effect decay for end-of-turn or other decay modes.
    /// Handles status effect expiration and stack reduction through StatusEffectLogicManager.
    /// </summary>
    public class ProcessStatusEffectDecayCommand : GameCommand
    {
        private readonly StatusEffectDecayMode _decayMode;

        public ProcessStatusEffectDecayCommand(StatusEffectDecayMode decayMode) : base(false)
        {
            _decayMode = decayMode;
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState == null) 
            {
                _logger.LogWarning("[ProcessStatusEffectDecayCommand] Cannot execute - no current state");
                return false;
            }

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            
            _logger.LogInfo($"[ProcessStatusEffectDecayCommand] Processing decay for mode: {_decayMode}");
            
            try
            {
                // Get current active effects count for logging
                var currentEffectsCount = currentState.StatusEffects.ActiveEffects.Length;
                
                // Use StatusEffectLogicManager to process decay
                var newStatusEffectsState = StatusEffectLogicManager.ProcessDecay(
                    currentState.StatusEffects,
                    _decayMode);

                var newState = currentState.WithStatusEffects(newStatusEffectsState);

                var newEffectsCount = newStatusEffectsState.ActiveEffects.Length;
                _logger.LogInfo($"[ProcessStatusEffectDecayCommand] Decay processed. Effects before: {currentEffectsCount}, after: {newEffectsCount}");
                
                token.Complete(CommandResult.Success(newState));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ProcessStatusEffectDecayCommand] Error processing decay: {ex.Message}");
                token.Complete(CommandResult.Failure($"Failed to process status effect decay: {ex.Message}"));
            }
        }

        public override string GetDescription()
        {
            return $"Process status effect decay for {_decayMode}";
        }

        /// <summary>
        /// Gets the decay mode for testing purposes
        /// </summary>
        public StatusEffectDecayMode GetDecayMode()
        {
            return _decayMode;
        }
    }
}