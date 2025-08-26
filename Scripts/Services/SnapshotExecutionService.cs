using System;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Managers;
using Scripts.State;

namespace Maximagus.Scripts.Services
{
    public class SnapshotExecutionService
    {
        private readonly ILogger _logger;
        private readonly CommandValidationService _validationService;

        public SnapshotExecutionService(ILogger logger, CommandValidationService validationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        }

        public string GetSpellIdFromState(IGameStateData gameState)
        {
            if (gameState.Spell.Properties.TryGetValue("SnapshotSpellId", out var spellIdValue))
            {
                return spellIdValue.AsString();
            }
            return null;
        }

        public string GenerateActionKey(ActionResource action, IGameStateData gameState)
        {
            return $"{action.ActionId}_{gameState.Spell.CurrentActionIndex}";
        }

        public EncounterStateSnapshot RetrieveAndValidateSnapshot(
            string spellId, 
            string actionKey, 
            string commandName)
        {
            _logger.LogInfo($"[{commandName}] Looking for snapshot: spell={spellId}, action={actionKey}");

            var snapshot = EncounterSnapshotManager.GetSnapshotForAction(spellId, actionKey);
            
            if (!_validationService.ValidateSnapshot(snapshot, spellId, actionKey, commandName))
            {
                LogAvailableSnapshots(spellId, commandName);
                return null;
            }

            return snapshot;
        }

        public void LogActionExecution(
            IGameStateData oldState, 
            IGameStateData newState, 
            EncounterStateSnapshot snapshot,
            ActionResource action,
            string cardId,
            string commandName)
        {
            try
            {
                _logger.LogInfo($"[{commandName}] Action Execution Details:");
                _logger.LogInfo($"  Action: {action.GetType().Name} (ID: {action.ActionId})");
                _logger.LogInfo($"  Card: {cardId}");
                _logger.LogInfo($"  Action Index: {oldState.Spell.CurrentActionIndex} -> {newState.Spell.CurrentActionIndex}");
                _logger.LogInfo($"  Total Damage: {oldState.Spell.TotalDamageDealt} -> {newState.Spell.TotalDamageDealt}");
                _logger.LogInfo($"  This Action Damage: {snapshot.ActionResult.FinalDamage}");
                _logger.LogInfo($"  Modifiers Consumed: {snapshot.ActionResult.ConsumedModifiers.Length}");
                _logger.LogInfo($"  Status Effects: {oldState.StatusEffects.ActiveEffects.Length} -> {newState.StatusEffects.ActiveEffects.Length}");
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"[{commandName}] Error logging action execution: {ex.Message}");
            }
        }

        public void LogSpellProperties(IGameStateData currentState, string commandName)
        {
            _logger.LogInfo($"[{commandName}] Available spell properties:");
            foreach (var prop in currentState.Spell.Properties)
            {
                _logger.LogInfo($"  {prop.Key}: {prop.Value}");
            }
        }

        private void LogAvailableSnapshots(string spellId, string commandName)
        {
            try
            {
                var allSnapshots = EncounterSnapshotManager.GetAllSnapshots(spellId);
                _logger.LogInfo($"[{commandName}] Available snapshots for spell {spellId}: {allSnapshots.Length}");
                foreach (var availableSnapshot in allSnapshots)
                {
                    _logger.LogInfo($"  - {availableSnapshot.ActionKey} (Created: {availableSnapshot.CreatedAt:HH:mm:ss.fff})");
                }
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"[{commandName}] Error logging available snapshots: {ex.Message}");
            }
        }
    }
}