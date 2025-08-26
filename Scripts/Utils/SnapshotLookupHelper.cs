using System;
using Godot;
using Scripts.State;
using Maximagus.Scripts.Managers;

namespace Scripts.Utilities
{
    
    public static class SnapshotLookupHelper
    {
        private static readonly ILogger _logger = ServiceLocator.GetService<ILogger>();

        
        public static EncounterStateSnapshot TryGetSnapshotForAction(
            IGameStateData gameState, 
            string actionId, 
            string logContext = "ActionResource")
        {
            try
            {
                // Validate inputs
                if (gameState?.Spell?.IsActive != true || string.IsNullOrEmpty(actionId))
                {
                    return null;
                }

                // Check if we have snapshot information available
                if (gameState.Spell.Properties?.TryGetValue("SnapshotSpellId", out var spellIdValue) == true)
                {
                    var spellId = spellIdValue.AsString();
                    if (string.IsNullOrEmpty(spellId))
                    {
                        return null;
                    }

                    var currentActionIndex = gameState.Spell.CurrentActionIndex;
                    
                    // Strategy 1: Look for the action that was just executed (most common case)
                    var justExecutedActionIndex = currentActionIndex - 1;
                    var executedActionKey = $"{actionId}_{justExecutedActionIndex}";
                    var snapshot = EncounterSnapshotManager.GetSnapshotForAction(spellId, executedActionKey);
                    
                    if (snapshot != null && snapshot.IsValid())
                    {
                        _logger.LogInfo($"[{logContext}] Found snapshot via just-executed strategy: {executedActionKey}");
                        return snapshot;
                    }

                    // Strategy 2: Look for the current action index (alternative timing)
                    _logger.LogInfo("Attempting snapshot lookup strategy 2: current action index");
                    var currentActionKey = $"{actionId}_{currentActionIndex}";
                    snapshot = EncounterSnapshotManager.GetSnapshotForAction(spellId, currentActionKey);
                    
                    if (snapshot != null && snapshot.IsValid())
                    {
                        _logger.LogInfo($"[{logContext}] Found snapshot via current-action strategy: {currentActionKey}");
                        return snapshot;
                    }

                    // Strategy 3: Search through all snapshots for this action ID (fallback)
                    _logger.LogInfo("Attempting snapshot lookup strategy 3: searching all snapshots");
                    var allSnapshots = EncounterSnapshotManager.GetAllSnapshots(spellId);
                    foreach (var candidateSnapshot in allSnapshots)
                    {
                        if (candidateSnapshot.ActionKey.StartsWith($"{actionId}_"))
                        {
                            if (candidateSnapshot.IsValid())
                            {
                                _logger.LogInfo($"[{logContext}] Found snapshot via search strategy: {candidateSnapshot.ActionKey}");
                                return candidateSnapshot;
                            }
                        }
                    }

                    // No valid snapshot found
                    _logger.LogInfo($"[{logContext}] No valid snapshot found for action {actionId} " +
                            $"(spell: {spellId}, currentIndex: {currentActionIndex})");
                    return null;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogInfo($"[{logContext}] Error during snapshot lookup for action {actionId}: {ex.Message}");
                return null;
            }
        }

        
        public static bool IsSnapshotBasedPopupAvailable(IGameStateData gameState)
        {
            return gameState?.Spell?.IsActive == true &&
                   gameState.Spell.Properties?.ContainsKey("SnapshotSpellId") == true;
        }

        
        public static string GetSpellIdFromGameState(IGameStateData gameState)
        {
            if (gameState?.Spell?.Properties?.TryGetValue("SnapshotSpellId", out var spellIdValue) == true)
            {
                return spellIdValue.AsString();
            }
            return null;
        }
    }
}