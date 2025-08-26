using System;
using Scripts.State;
using Godot;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Managers;
using Maximagus.Scripts.Services;
using System.Collections.Generic;

namespace Scripts.Commands.Spell
{
    public class ExecuteCardActionCommand : GameCommand
    {
        private readonly ActionResource _action;
        private readonly string _cardId;
        private readonly CommandValidationService _validationService;
        private readonly SnapshotExecutionService _snapshotService;

        public ExecuteCardActionCommand(ActionResource action, string cardId) : base()
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _cardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
            _validationService = SpellServiceContainer.ValidationService;
            _snapshotService = SpellServiceContainer.SnapshotExecutionService;
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState == null) return false;
            
            if (!_validationService.ValidateSpellIsActive(currentState, "ExecuteCardActionCommand"))
                return false;

            if (!_validationService.ValidateActionNotNull(_action, "ExecuteCardActionCommand"))
                return false;

            var spellId = _snapshotService.GetSpellIdFromState(currentState);
            if (!_validationService.ValidateSpellIdExists(spellId, "ExecuteCardActionCommand"))
                return false;

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            
            _logger.LogInfo($"[ExecuteCardActionCommand] Executing {_action.GetType().Name} for card {_cardId} using snapshot-based approach");
            
            ExecuteActionWithSnapshots(currentState, token);
        }

        private void ExecuteActionWithSnapshots(IGameStateData currentState, CommandCompletionToken token)
        {
            var followUpCommands = new List<GameCommand>();

            try
            {
                var spellId = _snapshotService.GetSpellIdFromState(currentState);
                if (string.IsNullOrEmpty(spellId))
                {
                    HandleMissingSpellId(currentState);
                    return;
                }

                var actionKey = _snapshotService.GenerateActionKey(_action, currentState);
                var snapshot = _snapshotService.RetrieveAndValidateSnapshot(spellId, actionKey, "ExecuteCardActionCommand");
                
                if (snapshot == null)
                {
                    var errorMessage = $"Failed to retrieve valid snapshot for spell {spellId}, action {actionKey}";
                    _logger.LogError($"[ExecuteCardActionCommand] {errorMessage}");
                    token.Complete(CommandResult.Failure(errorMessage));
                    return;
                }

                if (!_validationService.ValidateActionExecution(currentState, snapshot, "ExecuteCardActionCommand"))
                {
                    var errorMessage = $"State validation failed for spell {spellId}, action {actionKey}";
                    _logger.LogError($"[ExecuteCardActionCommand] {errorMessage}");
                    token.Complete(CommandResult.Failure(errorMessage));
                    return;
                }

                ApplySnapshotAndComplete(currentState, snapshot, followUpCommands, token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ExecuteCardActionCommand] Error executing action with snapshots: {ex.Message}");
                _logger.LogInfo($"[ExecuteCardActionCommand] Exception details: {ex}");
                token.Complete(CommandResult.Failure($"Failed to execute card action: {ex.Message}"));
            }
        }

        private void HandleMissingSpellId(IGameStateData currentState)
        {
            var errorMessage = "No spell ID found for snapshot retrieval. " +
                             "Ensure PreCalculateSpellCommand was executed and stored the spell ID.";
            _logger.LogError($"[ExecuteCardActionCommand] {errorMessage}");
            
            _snapshotService.LogSpellProperties(currentState, "ExecuteCardActionCommand");
            throw new InvalidOperationException(errorMessage);
        }

        private void ApplySnapshotAndComplete(
            IGameStateData currentState, 
            EncounterStateSnapshot snapshot, 
            List<GameCommand> followUpCommands,
            CommandCompletionToken token)
        {
            var newState = SpellLogicManager.ApplyEncounterSnapshot(currentState, snapshot);

            _logger.LogInfo($"[ExecuteCardActionCommand] Applied snapshot for action {_snapshotService.GenerateActionKey(_action, currentState)}: " +
                     $"{snapshot.ActionResult.FinalDamage} damage, " +
                     $"Action Index: {snapshot.ResultingState.ActionIndex}");
            
            _snapshotService.LogActionExecution(currentState, newState, snapshot, _action, _cardId, "ExecuteCardActionCommand");
            
            // Delay completion to preserve original timing (1.5s between actions)
            const float CardAnimationDelay = 1.5f;
            TimerUtils.ExecuteAfter(() => {
                token.Complete(CommandResult.Success(newState, followUpCommands));
            }, CardAnimationDelay);
        }

        public override string GetDescription()
        {
            return $"Execute {_action.GetType().Name} for card {_cardId} using pre-calculated snapshot";
        }
    }
}