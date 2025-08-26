using System;
using Scripts.State;
using Godot;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Managers;
using System.Collections.Generic;

namespace Scripts.Commands.Spell
{

    public class ExecuteCardActionCommand : GameCommand
    {
        private readonly ActionResource _action;
        private readonly string _cardId;

        public ExecuteCardActionCommand(ActionResource action, string cardId) : base(false)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
            _cardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState == null) return false;
            
            // Must have an active spell to execute actions
            if (!currentState.Spell.IsActive)
            {
                _logger.LogWarning("[ExecuteCardActionCommand] Cannot execute action - no active spell");
                return false;
            }

            if (_action == null)
            {
                _logger.LogWarning("[ExecuteCardActionCommand] Cannot execute - action is null");
                return false;
            }

            // Check if we have a spell ID for snapshot retrieval
            var spellId = GetSpellIdFromState(currentState);
            if (string.IsNullOrEmpty(spellId))
            {
                _logger.LogWarning("[ExecuteCardActionCommand] Cannot execute - no spell ID found for snapshot retrieval");
                return false;
            }

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            
            _logger.LogInfo($"[ExecuteCardActionCommand] Executing {_action.GetType().Name} for card {_cardId} using snapshot-based approach");
            
            // Execute the action using pre-calculated snapshots
            ExecuteActionWithSnapshots(currentState, token);
        }

        private void ExecuteActionWithSnapshots(IGameStateData currentState, CommandCompletionToken token)
        {
            var followUpCommands = new List<GameCommand>();

            try
            {
                // Get the spell ID for snapshot retrieval
                var spellId = GetSpellIdFromState(currentState);
                if (string.IsNullOrEmpty(spellId))
                {
                    var errorMessage = "No spell ID found for snapshot retrieval. " +
                                     "Ensure PreCalculateSpellCommand was executed and stored the spell ID.";
                    _logger.LogError($"[ExecuteCardActionCommand] {errorMessage}");
                    
                    // Debug: Print available spell properties
                    _logger.LogInfo("[ExecuteCardActionCommand] Available spell properties:");
                    foreach (var prop in currentState.Spell.Properties)
                    {
                        _logger.LogInfo($"  {prop.Key}: {prop.Value}");
                    }
                    
                    throw new InvalidOperationException(errorMessage);
                }

                // Generate the action key for this specific action
                var actionKey = GenerateActionKey(currentState);

                _logger.LogInfo($"[ExecuteCardActionCommand] Looking for snapshot: spell={spellId}, action={actionKey}");

                // Retrieve and apply the pre-calculated snapshot
                var snapshot = EncounterSnapshotManager.GetSnapshotForAction(spellId, actionKey);
                if (snapshot == null)
                {
                    var errorMessage = $"No pre-calculated snapshot found for spell {spellId}, action {actionKey}. " +
                                     "Ensure PreCalculateSpellCommand was executed before action execution.";
                    _logger.LogError($"[ExecuteCardActionCommand] {errorMessage}");
                    
                    // Debug: List all available snapshots for this spell
                    var allSnapshots = EncounterSnapshotManager.GetAllSnapshots(spellId);
                    _logger.LogInfo($"[ExecuteCardActionCommand] Available snapshots for spell {spellId}: {allSnapshots.Length}");
                    foreach (var availableSnapshot in allSnapshots)
                    {
                        _logger.LogInfo($"  - {availableSnapshot.ActionKey} (Created: {availableSnapshot.CreatedAt:HH:mm:ss.fff})");
                    }
                    
                    throw new InvalidOperationException(errorMessage);
                }

                // Validate the snapshot
                if (!snapshot.IsValid())
                {
                    var errorMessage = $"Invalid snapshot for spell {spellId}, action {actionKey}";
                    _logger.LogError($"[ExecuteCardActionCommand] {errorMessage}");
                    _logger.LogInfo($"[ExecuteCardActionCommand] Snapshot details: {snapshot}");
                    throw new InvalidOperationException(errorMessage);
                }

                // Validate state consistency before applying
                if (!ValidateActionExecution(currentState, snapshot))
                {
                    var errorMessage = $"State validation failed for spell {spellId}, action {actionKey}";
                    _logger.LogError($"[ExecuteCardActionCommand] {errorMessage}");
                    throw new InvalidOperationException(errorMessage);
                }

                // Apply the snapshot to get the new state
                var newState = SpellLogicManager.ApplyEncounterSnapshot(currentState, snapshot);

                _logger.LogInfo($"[ExecuteCardActionCommand] Applied snapshot for action {actionKey}: " +
                         $"{snapshot.ActionResult.FinalDamage} damage, " +
                         $"Action Index: {snapshot.ResultingState.ActionIndex}");
                
                // Log detailed state changes for debugging
                LogActionExecution(currentState, newState, snapshot);
                
                // Delay completion to preserve original timing (1.5s between actions)
                const float CardAnimationDelay = 1.5f;
                TimerUtils.ExecuteAfter(() => {
                    token.Complete(CommandResult.Success(newState, followUpCommands));
                }, CardAnimationDelay);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ExecuteCardActionCommand] Error executing action with snapshots: {ex.Message}");
                _logger.LogInfo($"[ExecuteCardActionCommand] Exception details: {ex}");
                token.Complete(CommandResult.Failure($"Failed to execute card action: {ex.Message}"));
            }
        }

        private bool ValidateActionExecution(IGameStateData currentState, EncounterStateSnapshot snapshot)
        {
            try
            {
                // Validate that the spell is still active
                if (!currentState.Spell.IsActive)
                {
                    _logger.LogInfo("[ExecuteCardActionCommand] Validation failed: No active spell");
                    return false;
                }

                // Validate that we're executing actions in the correct order
                var currentActionIndex = currentState.Spell.CurrentActionIndex;
                var expectedActionIndex = snapshot.ResultingState.ActionIndex;
                
                if (expectedActionIndex != currentActionIndex + 1)
                {
                    _logger.LogInfo($"[ExecuteCardActionCommand] Validation failed: " +
                             $"Expected action index {currentActionIndex + 1}, snapshot has {expectedActionIndex}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"[ExecuteCardActionCommand] Error validating action execution: {ex.Message}");
                return false;
            }
        }

        private void LogActionExecution(IGameStateData oldState, IGameStateData newState, EncounterStateSnapshot snapshot)
        {
            try
            {
                _logger.LogInfo($"[ExecuteCardActionCommand] Action Execution Details:");
                _logger.LogInfo($"  Action: {_action.GetType().Name} (ID: {_action.ActionId})");
                _logger.LogInfo($"  Card: {_cardId}");
                _logger.LogInfo($"  Action Index: {oldState.Spell.CurrentActionIndex} -> {newState.Spell.CurrentActionIndex}");
                _logger.LogInfo($"  Total Damage: {oldState.Spell.TotalDamageDealt} -> {newState.Spell.TotalDamageDealt}");
                _logger.LogInfo($"  This Action Damage: {snapshot.ActionResult.FinalDamage}");
                _logger.LogInfo($"  Modifiers Consumed: {snapshot.ActionResult.ConsumedModifiers.Length}");
                _logger.LogInfo($"  Status Effects: {oldState.StatusEffects.ActiveEffects.Length} -> {newState.StatusEffects.ActiveEffects.Length}");
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"[ExecuteCardActionCommand] Error logging action execution: {ex.Message}");
            }
        }

        private string GetSpellIdFromState(IGameStateData gameState)
        {
            // Retrieve the spell ID that was stored during pre-calculation
            if (gameState.Spell.Properties.TryGetValue("SnapshotSpellId", out var spellIdValue))
            {
                return spellIdValue.AsString();
            }

            return null;
        }

        private string GenerateActionKey(IGameStateData gameState)
        {
            // Generate the same action key format used during pre-calculation
            // This should match the key generation in PreCalculateActionWithSnapshot
            return $"{_action.ActionId}_{gameState.Spell.CurrentActionIndex}";
        }

        public override string GetDescription()
        {
            return $"Execute {_action.GetType().Name} for card {_cardId} using pre-calculated snapshot";
        }
    }
}