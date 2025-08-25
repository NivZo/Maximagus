using System;
using Scripts.State;
using Godot;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Spell
{
    /// <summary>
    /// Command to cancel an active spell and clean up associated resources.
    /// Clears the active spell state and snapshots without creating a history entry.
    /// </summary>
    public class CancelSpellCommand : GameCommand
    {
        private readonly string _reason;

        public CancelSpellCommand(string reason = "User cancelled") : base(false)
        {
            _reason = reason ?? "Cancelled";
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState == null) return false;
            
            // Must have an active spell to cancel
            if (!currentState.Spell.IsActive)
            {
                _logger.LogWarning("[CancelSpellCommand] Cannot cancel spell - no active spell");
                return false;
            }

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            var currentSpell = currentState.Spell;
            
            _logger.LogInfo($"[CancelSpellCommand] Cancelling active spell - Reason: {_reason}");
            
            try
            {
                // Clean up snapshots for this spell
                CleanupSpellSnapshots(currentSpell);

                // Create a new inactive spell state (no history entry for cancelled spells)
                var newSpellState = SpellState.CreateInitial();
                var newState = currentState.WithSpell(newSpellState);

                _logger.LogInfo($"[CancelSpellCommand] Spell cancelled successfully");
                
                token.Complete(CommandResult.Success(newState));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[CancelSpellCommand] Error cancelling spell: {ex.Message}");
                
                // Still try to clean up snapshots and reset state
                try
                {
                    CleanupSpellSnapshots(currentSpell);
                    var errorSpellState = SpellState.CreateInitial();
                    var errorState = currentState.WithSpell(errorSpellState);
                    
                    token.Complete(CommandResult.Success(errorState));
                }
                catch
                {
                    token.Complete(CommandResult.Failure($"Failed to cancel spell: {ex.Message}"));
                }
            }
        }

        private void CleanupSpellSnapshots(SpellState spellState)
        {
            try
            {
                // Get the spell ID used for snapshots
                if (spellState.Properties.TryGetValue("SnapshotSpellId", out var spellIdValue))
                {
                    var spellId = spellIdValue.AsString();
                    if (!string.IsNullOrEmpty(spellId))
                    {
                        EncounterSnapshotManager.ClearSnapshots(spellId);
                        _logger.LogInfo($"[CancelSpellCommand] Cleaned up snapshots for cancelled spell {spellId}");
                    }
                }
                
                // Also perform general cleanup to prevent memory leaks
                EncounterSnapshotManager.AutoCleanup();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[CancelSpellCommand] Error cleaning up snapshots: {ex.Message}");
                // Don't fail the spell cancellation due to snapshot cleanup issues
            }
        }

        public override string GetDescription()
        {
            return $"Cancel active spell - Reason: {_reason}";
        }
    }
}