using System;
using Maximagus.Scripts.Managers;
using Scripts.State;

namespace Maximagus.Scripts.Services
{
    public class CommandValidationService
    {
        private readonly ILogger _logger;

        public CommandValidationService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool ValidateSpellIsActive(IGameStateData currentState, string commandName)
        {
            if (currentState?.Spell?.IsActive != true)
            {
                _logger.LogWarning($"[{commandName}] Cannot execute - no active spell");
                return false;
            }
            return true;
        }

        public bool ValidateActionNotNull(object action, string commandName)
        {
            if (action == null)
            {
                _logger.LogWarning($"[{commandName}] Cannot execute - action is null");
                return false;
            }
            return true;
        }

        public bool ValidateSpellIdExists(string spellId, string commandName)
        {
            if (string.IsNullOrEmpty(spellId))
            {
                _logger.LogWarning($"[{commandName}] Cannot execute - no spell ID found for snapshot retrieval");
                return false;
            }
            return true;
        }

        public bool ValidateActionExecution(IGameStateData currentState, EncounterStateSnapshot snapshot, string commandName)
        {
            try
            {
                if (!currentState.Spell.IsActive)
                {
                    _logger.LogInfo($"[{commandName}] Validation failed: No active spell");
                    return false;
                }

                var currentActionIndex = currentState.Spell.CurrentActionIndex;
                var expectedActionIndex = snapshot.ResultingState.ActionIndex;
                
                if (expectedActionIndex != currentActionIndex + 1)
                {
                    _logger.LogInfo($"[{commandName}] Validation failed: " +
                             $"Expected action index {currentActionIndex + 1}, snapshot has {expectedActionIndex}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"[{commandName}] Error validating action execution: {ex.Message}");
                return false;
            }
        }

        public bool ValidateSnapshot(EncounterStateSnapshot snapshot, string spellId, string actionKey, string commandName)
        {
            if (snapshot == null)
            {
                _logger.LogError($"[{commandName}] No pre-calculated snapshot found for spell {spellId}, action {actionKey}. " +
                               "Ensure PreCalculateSpellCommand was executed before action execution.");
                return false;
            }

            if (!snapshot.IsValid())
            {
                _logger.LogError($"[{commandName}] Invalid snapshot for spell {spellId}, action {actionKey}");
                _logger.LogInfo($"[{commandName}] Snapshot details: {snapshot}");
                return false;
            }

            return true;
        }
    }
}