using System;
using Scripts.State;
using Godot;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Spell
{
    /// <summary>
    /// Command to apply a pre-calculated EncounterState snapshot to the current game state.
    /// This ensures atomic application of both spell and status effect state changes from snapshots.
    /// </summary>
    public class ApplyEncounterSnapshotCommand : GameCommand
    {
        private readonly string _spellId;
        private readonly string _actionKey;

        public ApplyEncounterSnapshotCommand(string spellId, string actionKey) : base(false)
        {
            _spellId = spellId ?? throw new ArgumentNullException(nameof(spellId));
            _actionKey = actionKey ?? throw new ArgumentNullException(nameof(actionKey));
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState == null) return false;
            
            // Must have an active spell to apply snapshots
            if (!currentState.Spell.IsActive)
            {
                _logger.LogWarning("[ApplyEncounterSnapshotCommand] Cannot apply snapshot - no active spell");
                return false;
            }

            // Check if the snapshot exists
            var snapshot = EncounterSnapshotManager.GetSnapshotForAction(_spellId, _actionKey);
            if (snapshot == null)
            {
                _logger.LogWarning($"[ApplyEncounterSnapshotCommand] Cannot apply snapshot - no snapshot found for spell {_spellId}, action {_actionKey}");
                return false;
            }

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            
            GD.Print($"[ApplyEncounterSnapshotCommand] Applying snapshot for spell {_spellId}, action {_actionKey}");
            
            try
            {
                // Retrieve the pre-calculated snapshot
                var snapshot = EncounterSnapshotManager.GetSnapshotForAction(_spellId, _actionKey);
                
                if (snapshot == null)
                {
                    var errorMessage = $"No pre-calculated snapshot found for spell {_spellId}, action {_actionKey}";
                    _logger.LogError($"[ApplyEncounterSnapshotCommand] {errorMessage}");
                    
                    // Debug information to help troubleshoot missing snapshots
                    var allSnapshots = EncounterSnapshotManager.GetAllSnapshots(_spellId);
                    GD.Print($"[ApplyEncounterSnapshotCommand] Available snapshots for spell {_spellId}: {allSnapshots.Length}");
                    foreach (var availableSnapshot in allSnapshots)
                    {
                        GD.Print($"  - {availableSnapshot.ActionKey} (Created: {availableSnapshot.CreatedAt:HH:mm:ss.fff})");
                    }
                    
                    token.Complete(CommandResult.Failure(errorMessage));
                    return;
                }

                // Validate the snapshot before applying
                if (!snapshot.IsValid())
                {
                    var errorMessage = $"Invalid snapshot for spell {_spellId}, action {_actionKey}";
                    _logger.LogError($"[ApplyEncounterSnapshotCommand] {errorMessage}");
                    GD.Print($"[ApplyEncounterSnapshotCommand] Snapshot details: {snapshot}");
                    token.Complete(CommandResult.Failure(errorMessage));
                    return;
                }

                // Validate state transition consistency
                if (!ValidateStateTransition(currentState, snapshot))
                {
                    var errorMessage = $"Invalid state transition for spell {_spellId}, action {_actionKey}";
                    _logger.LogError($"[ApplyEncounterSnapshotCommand] {errorMessage}");
                    token.Complete(CommandResult.Failure(errorMessage));
                    return;
                }

                // Apply the snapshot to the current game state
                var newState = SpellLogicManager.ApplyEncounterSnapshot(currentState, snapshot);

                GD.Print($"[ApplyEncounterSnapshotCommand] Successfully applied snapshot: " +
                         $"Action {_actionKey} -> {snapshot.ActionResult.FinalDamage} damage, " +
                         $"Action Index: {snapshot.ResultingState.ActionIndex}");
                
                // Debug logging for state changes
                LogStateChanges(currentState, newState, snapshot);
                
                token.Complete(CommandResult.Success(newState));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ApplyEncounterSnapshotCommand] Error applying snapshot: {ex.Message}");
                GD.Print($"[ApplyEncounterSnapshotCommand] Exception details: {ex}");
                token.Complete(CommandResult.Failure($"Failed to apply encounter snapshot: {ex.Message}"));
            }
        }

        private bool ValidateStateTransition(IGameStateData currentState, EncounterStateSnapshot snapshot)
        {
            try
            {
                // Validate that the spell is still active
                if (!currentState.Spell.IsActive)
                {
                    GD.Print("[ApplyEncounterSnapshotCommand] State transition validation failed: No active spell");
                    return false;
                }

                // Validate that the action index progression is correct
                var expectedActionIndex = currentState.Spell.CurrentActionIndex;
                if (snapshot.ResultingState.ActionIndex != expectedActionIndex + 1)
                {
                    GD.Print($"[ApplyEncounterSnapshotCommand] State transition validation failed: " +
                             $"Expected action index {expectedActionIndex + 1}, got {snapshot.ResultingState.ActionIndex}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                GD.Print($"[ApplyEncounterSnapshotCommand] Error validating state transition: {ex.Message}");
                return false;
            }
        }

        private void LogStateChanges(IGameStateData oldState, IGameStateData newState, EncounterStateSnapshot snapshot)
        {
            try
            {
                GD.Print($"[ApplyEncounterSnapshotCommand] State Changes:");
                GD.Print($"  Action Index: {oldState.Spell.CurrentActionIndex} -> {newState.Spell.CurrentActionIndex}");
                GD.Print($"  Total Damage: {oldState.Spell.TotalDamageDealt} -> {newState.Spell.TotalDamageDealt}");
                GD.Print($"  Status Effects Count: {oldState.StatusEffects.ActiveEffects.Length} -> {newState.StatusEffects.ActiveEffects.Length}");
                GD.Print($"  Snapshot Result: {snapshot.ActionResult.FinalDamage} damage, {snapshot.ActionResult.ConsumedModifiers.Length} modifiers consumed");
            }
            catch (Exception ex)
            {
                GD.Print($"[ApplyEncounterSnapshotCommand] Error logging state changes: {ex.Message}");
            }
        }

        public override string GetDescription()
        {
            return $"Apply EncounterState snapshot for spell {_spellId}, action {_actionKey}";
        }
    }
}