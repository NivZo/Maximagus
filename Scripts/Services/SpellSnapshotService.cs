using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;
using Scripts.Interfaces.Services;
using Scripts.State;
using Scripts.Utils;

namespace Maximagus.Scripts.Services
{
    public class SpellSnapshotService : ISpellSnapshotService
    {
        private readonly ILogger _logger;

        public SpellSnapshotService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public EncounterStateSnapshot PreCalculateActionWithSnapshot(
            ActionResource action,
            EncounterState currentEncounterState)
        {
            CommonValidation.ThrowIfNull(action, nameof(action));
            CommonValidation.ThrowIfNull(currentEncounterState, nameof(currentEncounterState));

            var actionResult = SpellLogicManager.PreCalculateActionResult(action, currentEncounterState);
            
            var resultingEncounterState = SpellServiceContainer.StateService.SimulateActionEffectsOnEncounterState(
                currentEncounterState,
                action,
                actionResult);
            
            var actionKey = $"{action.ActionId}_{currentEncounterState.ActionIndex}";
            
            return EncounterStateSnapshot.Create(actionKey, resultingEncounterState, actionResult);
        }

        public ImmutableArray<EncounterStateSnapshot> PreCalculateSpellWithSnapshots(
            IGameStateData initialGameState,
            IEnumerable<CardState> playedCards)
        {
            if (initialGameState == null)
                throw new ArgumentNullException(nameof(initialGameState));
            if (playedCards == null)
                throw new ArgumentNullException(nameof(playedCards));

            var snapshots = ImmutableArray.CreateBuilder<EncounterStateSnapshot>();
            var currentEncounterState = EncounterState.FromGameState(initialGameState, DateTime.UtcNow);
            var actionIndex = 0;
            
            var allActions = playedCards.SelectMany(c => c.Resource.Actions ?? Enumerable.Empty<ActionResource>()).ToList();
            _logger.LogInfo($"[SpellSnapshotService] Pre-calculating spell with {allActions.Count} actions using EncounterState snapshots");

            foreach (var playedCard in playedCards)
            {
                if (playedCard.Resource.Actions == null) continue;
                
                foreach (var action in playedCard.Resource.Actions)
                {
                    _logger.LogInfo("\n-----------------------------------------------------------------------");
                    
                    currentEncounterState = currentEncounterState.WithActionIndex(actionIndex);
                    
                    var snapshot = PreCalculateActionWithSnapshot(action, currentEncounterState);
                    snapshots.Add(snapshot);
                    
                    currentEncounterState = snapshot.ResultingState;
                    
                    _logger.LogInfo($"[SpellSnapshotService] Created snapshot for action {actionIndex}: {action.GetType().Name} -> {snapshot.ActionResult.FinalDamage} damage");
                    _logger.LogInfo("-----------------------------------------------------------------------");
                    
                    actionIndex++;
                }
            }

            _logger.LogInfo($"[SpellSnapshotService] Completed spell pre-calculation with {snapshots.Count} snapshots");
            return snapshots.ToImmutable();
        }

        public IGameStateData ApplyEncounterSnapshot(
            IGameStateData gameState,
            EncounterStateSnapshot snapshot)
        {
            if (gameState == null)
                throw new ArgumentNullException(nameof(gameState));
            if (snapshot == null)
                throw new ArgumentNullException(nameof(snapshot));

            if (!snapshot.IsValid())
                throw new ArgumentException("Invalid snapshot provided", nameof(snapshot));

            _logger.LogInfo($"[SpellSnapshotService] Applying encounter snapshot: {snapshot.ActionKey}");
            
            // Get the spell state from the snapshot
            var snapshotSpellState = snapshot.ResultingState.Spell;
            
            // Preserve important properties from the current spell state that aren't in the snapshot
            var currentSpellState = gameState.Spell;
            var preservedProperties = snapshotSpellState.Properties;
            
            // Preserve the SnapshotSpellId if it exists in the current state but not in the snapshot
            if (currentSpellState.Properties.TryGetValue("SnapshotSpellId", out var spellIdValue) &&
                !snapshotSpellState.Properties.ContainsKey("SnapshotSpellId"))
            {
                preservedProperties = preservedProperties.SetItem("SnapshotSpellId", spellIdValue);
            }
            
            // Create the updated spell state with preserved properties
            var updatedSpellState = new SpellState(
                snapshotSpellState.IsActive,
                preservedProperties,
                snapshotSpellState.ActiveModifiers,
                snapshotSpellState.TotalDamageDealt,
                snapshotSpellState.History,
                snapshotSpellState.StartTime,
                snapshotSpellState.CurrentActionIndex);
            
            // Apply the updated spell state and status effects from the snapshot
            return gameState
                .WithSpell(updatedSpellState)
                .WithStatusEffects(snapshot.ResultingState.StatusEffects);
        }
    }
}