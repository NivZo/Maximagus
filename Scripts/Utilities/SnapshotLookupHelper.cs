using System;
using Godot;
using Scripts.State;
using Maximagus.Scripts.Managers;

namespace Scripts.Utilities
{
    /// <summary>
    /// Utility class for looking up EncounterState snapshots for action popup text generation.
    /// Follows SOLID principles by providing a single responsibility for snapshot retrieval
    /// with consistent lookup strategies across all action types.
    /// </summary>
    public static class SnapshotLookupHelper
    {
        private static readonly ILogger _logger = ServiceLocator.GetService<ILogger>();

        /// <summary>
        /// Attempts to find a snapshot for the given action using multiple lookup strategies.
        /// This method encapsulates the common snapshot lookup logic used by all action types.
        /// </summary>
        /// <param name="gameState">The current game state containing spell information</param>
        /// <param name="actionId">The ID of the action to find a snapshot for</param>
        /// <param name="logContext">Context string for logging (e.g., "DamageActionResource")</param>
        /// <returns>The found snapshot, or null if no valid snapshot is available</returns>
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

        /// <summary>
        /// Checks if snapshot-based popup text is available for the given game state.
        /// This is a lightweight check that can be used to determine whether to attempt
        /// snapshot-based calculations or fall back to static text.
        /// </summary>
        /// <param name="gameState">The current game state</param>
        /// <returns>True if snapshots are potentially available, false otherwise</returns>
        public static bool IsSnapshotBasedPopupAvailable(IGameStateData gameState)
        {
            return gameState?.Spell?.IsActive == true &&
                   gameState.Spell.Properties?.ContainsKey("SnapshotSpellId") == true;
        }

        /// <summary>
        /// Gets the spell ID from the game state for snapshot operations.
        /// Encapsulates the property access logic for consistency.
        /// </summary>
        /// <param name="gameState">The current game state</param>
        /// <returns>The spell ID if available, null otherwise</returns>
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