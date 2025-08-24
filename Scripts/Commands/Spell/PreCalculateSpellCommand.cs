using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.State;
using Godot;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Managers;
using System.Collections.Immutable;

namespace Scripts.Commands.Spell
{
    /// <summary>
    /// Command to pre-calculate all action results for a complete spell sequence using EncounterState snapshots.
    /// This generates complete EncounterState snapshots for all spell actions, ensuring that dependent 
    /// calculations (like PerChill damage) use the correct cumulative state from previous actions.
    /// </summary>
    public class PreCalculateSpellCommand : GameCommand
    {
        private readonly IReadOnlyList<ActionResource> _allActions;

        public PreCalculateSpellCommand(IReadOnlyList<ActionResource> allActions) : base(false)
        {
            _allActions = allActions ?? throw new ArgumentNullException(nameof(allActions));
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState == null) return false;
            
            // Must have an active spell to pre-calculate actions
            if (!currentState.Spell.IsActive)
            {
                _logger.LogWarning("[PreCalculateSpellCommand] Cannot pre-calculate - no active spell");
                return false;
            }

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            
            // Get played cards to map actions to cards
            var playedCards = currentState.Cards.PlayedCards?.OrderBy(c => c.Position).ToArray();
            if (playedCards == null || playedCards.Length == 0)
            {
                var errorMessage = "No played cards found for pre-calculation";
                _logger.LogError($"[PreCalculateSpellCommand] {errorMessage}");
                token.Complete(CommandResult.Failure(errorMessage));
                return;
            }
            
            GD.Print($"[PreCalculateSpellCommand] Pre-calculating EncounterState snapshots for {playedCards.Length} cards");
            
            // Log detailed information about the spell being pre-calculated
            LogSpellPreCalculationStart(currentState, playedCards);
            
            try
            {
                // Validate that we have actions to pre-calculate
                var totalActions = playedCards.Sum(c => c.Resource.Actions?.Count ?? 0);
                if (totalActions == 0)
                {
                    var errorMessage = "No actions found in played cards for pre-calculation";
                    _logger.LogError($"[PreCalculateSpellCommand] {errorMessage}");
                    token.Complete(CommandResult.Failure(errorMessage));
                    return;
                }

                // Generate EncounterState snapshots for all actions in the spell
                var snapshots = SpellLogicManager.PreCalculateSpellWithSnapshots(currentState, playedCards);
                
                // Validate that we generated the expected number of snapshots
                if (snapshots.Length != totalActions)
                {
                    var errorMessage = $"Snapshot count mismatch: expected {totalActions}, generated {snapshots.Length}";
                    _logger.LogError($"[PreCalculateSpellCommand] {errorMessage}");
                    token.Complete(CommandResult.Failure(errorMessage));
                    return;
                }

                // Validate all snapshots are valid
                for (int i = 0; i < snapshots.Length; i++)
                {
                    if (!snapshots[i].IsValid())
                    {
                        var errorMessage = $"Invalid snapshot generated at index {i}: {snapshots[i]}";
                        _logger.LogError($"[PreCalculateSpellCommand] {errorMessage}");
                        token.Complete(CommandResult.Failure(errorMessage));
                        return;
                    }
                }
                
                // Generate a unique spell ID for this casting
                var spellId = GenerateSpellId(currentState);
                
                // Store the snapshots for later retrieval during action execution
                EncounterSnapshotManager.StoreSnapshots(spellId, snapshots);
                
                // Update the spell state to include the spell ID for snapshot retrieval
                var updatedSpellState = currentState.Spell.WithProperty("SnapshotSpellId", Variant.From(spellId));
                var newState = currentState.WithSpell(updatedSpellState);

                GD.Print($"[PreCalculateSpellCommand] Successfully generated and stored {snapshots.Length} EncounterState snapshots for spell {spellId}");
                
                // Log detailed snapshot information for debugging
                LogSnapshotDetails(spellId, snapshots);
                
                token.Complete(CommandResult.Success(newState));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[PreCalculateSpellCommand] Error pre-calculating spell snapshots: {ex.Message}");
                GD.Print($"[PreCalculateSpellCommand] Exception details: {ex}");
                token.Complete(CommandResult.Failure($"Failed to pre-calculate spell snapshots: {ex.Message}"));
            }
        }

        private void LogSpellPreCalculationStart(IGameStateData gameState, CardState[] playedCards)
        {
            try
            {
                GD.Print($"[PreCalculateSpellCommand] Spell Pre-Calculation Details:");
                GD.Print($"  Active Spell: {gameState.Spell.IsActive}");
                GD.Print($"  Current Action Index: {gameState.Spell.CurrentActionIndex}");
                GD.Print($"  Total Damage So Far: {gameState.Spell.TotalDamageDealt}");
                GD.Print($"  Status Effects Count: {gameState.StatusEffects.ActiveEffects.Length}");
                GD.Print($"  Cards to Process: {playedCards.Length}");
                
                for (int i = 0; i < playedCards.Length; i++)
                {
                    var card = playedCards[i];
                    var actionCount = card.Resource.Actions?.Count ?? 0;
                    GD.Print($"    Card {i + 1}: {card.Resource.CardName} ({actionCount} actions)");
                }
            }
            catch (Exception ex)
            {
                GD.Print($"[PreCalculateSpellCommand] Error logging pre-calculation start: {ex.Message}");
            }
        }

        private void LogSnapshotDetails(string spellId, System.Collections.Immutable.ImmutableArray<EncounterStateSnapshot> snapshots)
        {
            try
            {
                GD.Print($"[PreCalculateSpellCommand] Generated Snapshots for Spell {spellId}:");
                for (int i = 0; i < snapshots.Length; i++)
                {
                    var snapshot = snapshots[i];
                    GD.Print($"  Snapshot {i + 1}: {snapshot.ActionKey}");
                    GD.Print($"    Damage: {snapshot.ActionResult.FinalDamage}");
                    GD.Print($"    Action Index: {snapshot.ResultingState.ActionIndex}");
                    GD.Print($"    Modifiers Consumed: {snapshot.ActionResult.ConsumedModifiers.Length}");
                    GD.Print($"    Created: {snapshot.CreatedAt:HH:mm:ss.fff}");
                }
            }
            catch (Exception ex)
            {
                GD.Print($"[PreCalculateSpellCommand] Error logging snapshot details: {ex.Message}");
            }
        }

        private string GenerateSpellId(IGameStateData gameState)
        {
            // Generate a unique spell ID based on timestamp and spell state
            var timestamp = DateTime.UtcNow.Ticks;
            var actionCount = _allActions.Count;
            return $"spell_{timestamp}_{actionCount}";
        }

        public override string GetDescription()
        {
            return $"Pre-calculate EncounterState snapshots for {_allActions.Count} spell actions";
        }
    }
}