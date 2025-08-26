using System;
using System.Linq;
using Scripts.State;
using Godot;
using Maximagus.Scripts.Spells.Abstractions;
using System.Collections.Generic;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Spell
{

    public class CompleteSpellCommand : GameCommand
    {
        private readonly IReadOnlyList<SpellCardResource> _castCards;
        private readonly bool _wasSuccessful;
        private readonly string _errorMessage;

        public CompleteSpellCommand(
            IReadOnlyList<SpellCardResource> castCards = null, 
            bool wasSuccessful = true, 
            string errorMessage = null) : base(false)
        {
            _castCards = castCards ?? new List<SpellCardResource>();
            _wasSuccessful = wasSuccessful;
            _errorMessage = errorMessage;
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState == null) return false;
            
            // Must have an active spell to complete
            if (!currentState.Spell.IsActive)
            {
                _logger.LogWarning("[CompleteSpellCommand] Cannot complete spell - no active spell");
                return false;
            }

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            var currentSpell = currentState.Spell;
            
            _logger.LogInfo($"[CompleteSpellCommand] Completing spell - Success: {_wasSuccessful}, Total Damage: {currentSpell.TotalDamageDealt}");
            
            try
            {
                // Clean up snapshots for this spell
                CleanupSpellSnapshots(currentSpell);

                // Create history entry from current spell state
                var historyEntry = new SpellHistoryEntry(
                    completedAt: DateTime.UtcNow,
                    totalDamage: currentSpell.TotalDamageDealt,
                    finalProperties: currentSpell.Properties,
                    castCardIds: System.Collections.Immutable.ImmutableArray.Create(_castCards.Select(card => card.CardResourceId).ToArray()),
                    castCardResources: System.Collections.Immutable.ImmutableArray.Create(_castCards.ToArray()),
                    wasSuccessful: _wasSuccessful,
                    errorMessage: _errorMessage);

                // Move spell to history and clear active state
                var newSpellState = currentSpell.WithCompletedSpell(historyEntry);
                var newState = currentState.WithSpell(newSpellState);

                _logger.LogInfo($"[CompleteSpellCommand] Spell completed and moved to history. History count: {newSpellState.History.Length}");
                
                token.Complete(CommandResult.Success(newState));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[CompleteSpellCommand] Error completing spell: {ex.Message}");
                
                // Still try to clean up snapshots and create a history entry with error information
                try
                {
                    CleanupSpellSnapshots(currentSpell);

                    var errorHistoryEntry = new SpellHistoryEntry(
                        completedAt: DateTime.UtcNow,
                        totalDamage: currentSpell.TotalDamageDealt,
                        finalProperties: currentSpell.Properties,
                        castCardIds: System.Collections.Immutable.ImmutableArray.Create(_castCards.Select(card => card.CardResourceId).ToArray()),
                        castCardResources: System.Collections.Immutable.ImmutableArray.Create(_castCards.ToArray()),
                        wasSuccessful: false,
                        errorMessage: ex.Message);

                    var errorSpellState = currentSpell.WithCompletedSpell(errorHistoryEntry);
                    var errorState = currentState.WithSpell(errorSpellState);
                    
                    token.Complete(CommandResult.Success(errorState));
                }
                catch
                {
                    token.Complete(CommandResult.Failure($"Failed to complete spell: {ex.Message}"));
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
                        _logger.LogInfo($"[CompleteSpellCommand] Cleaned up snapshots for spell {spellId}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"[CompleteSpellCommand] Error cleaning up snapshots: {ex.Message}");
                // Don't fail the spell completion due to snapshot cleanup issues
            }
        }

        public override string GetDescription()
        {
            var cardNames = _castCards.Any() ? string.Join(", ", _castCards.Select(c => c.CardName)) : "no cards";
            return $"Complete spell with {cardNames} - Success: {_wasSuccessful}";
        }
    }
}