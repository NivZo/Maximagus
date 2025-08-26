using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Scripts.State;

namespace Maximagus.Scripts.Managers
{

    public static class EncounterSnapshotManager
    {
        private static readonly ILogger _logger = ServiceLocator.GetService<ILogger>();
        
        // Thread-safe storage for snapshots by spell ID
        private static readonly ConcurrentDictionary<string, ImmutableArray<EncounterStateSnapshot>> _snapshots
            = new ConcurrentDictionary<string, ImmutableArray<EncounterStateSnapshot>>();
        
        // Optimized lookup cache for action keys within spells
        private static readonly ConcurrentDictionary<string, ImmutableDictionary<string, EncounterStateSnapshot>> _actionLookupCache
            = new ConcurrentDictionary<string, ImmutableDictionary<string, EncounterStateSnapshot>>();
        public static void StoreSnapshots(
            string spellId,
            ImmutableArray<EncounterStateSnapshot> snapshots)
        {
            if (string.IsNullOrEmpty(spellId))
                throw new ArgumentException("Spell ID cannot be null or empty", nameof(spellId));
            
            if (snapshots.IsDefault)
                snapshots = ImmutableArray<EncounterStateSnapshot>.Empty;

            _snapshots.AddOrUpdate(spellId, snapshots, (key, existing) => snapshots);
            
            // Build optimized lookup cache for this spell
            var lookupDict = snapshots.ToImmutableDictionary(s => s.ActionKey, s => s);
            _actionLookupCache.AddOrUpdate(spellId, lookupDict, (key, existing) => lookupDict);
            
            _logger.LogInfo($"[EncounterSnapshotManager] Stored {snapshots.Length} snapshots for spell {spellId} with optimized lookup");
        }
        public static EncounterStateSnapshot GetSnapshotForAction(
            string spellId,
            string actionKey)
        {
            if (string.IsNullOrEmpty(spellId))
                throw new ArgumentException("Spell ID cannot be null or empty", nameof(spellId));
            
            if (string.IsNullOrEmpty(actionKey))
                throw new ArgumentException("Action key cannot be null or empty", nameof(actionKey));

            // Try optimized lookup first
            if (_actionLookupCache.TryGetValue(spellId, out var lookupDict))
            {
                if (lookupDict.TryGetValue(actionKey, out var snapshot))
                {
                    _logger.LogInfo($"[EncounterSnapshotManager] Retrieved snapshot for action {actionKey} in spell {spellId} (optimized)");
                    return snapshot;
                }
            }

            // Fallback to linear search if cache miss
            if (_snapshots.TryGetValue(spellId, out var snapshots))
            {
                var snapshot = snapshots.FirstOrDefault(s => s.ActionKey == actionKey);
                
                if (snapshot == null)
                {
                    _logger.LogInfo($"[EncounterSnapshotManager] No snapshot found for action {actionKey} in spell {spellId}");
                }
                else
                {
                    _logger.LogInfo($"[EncounterSnapshotManager] Retrieved snapshot for action {actionKey} in spell {spellId} (fallback)");
                }

                return snapshot;
            }

            _logger.LogInfo($"[EncounterSnapshotManager] No snapshots found for spell {spellId}");
            return null;
        }
        public static ImmutableArray<EncounterStateSnapshot> GetAllSnapshots(string spellId)
        {
            if (string.IsNullOrEmpty(spellId))
                throw new ArgumentException("Spell ID cannot be null or empty", nameof(spellId));

            if (_snapshots.TryGetValue(spellId, out var snapshots))
            {
                _logger.LogInfo($"[EncounterSnapshotManager] Retrieved {snapshots.Length} snapshots for spell {spellId}");
                return snapshots;
            }

            _logger.LogInfo($"[EncounterSnapshotManager] No snapshots found for spell {spellId}");
            return ImmutableArray<EncounterStateSnapshot>.Empty;
        }
        public static void ClearSnapshots(string spellId)
        {
            if (string.IsNullOrEmpty(spellId))
                throw new ArgumentException("Spell ID cannot be null or empty", nameof(spellId));

            var snapshotsRemoved = _snapshots.TryRemove(spellId, out var removedSnapshots);
            var cacheRemoved = _actionLookupCache.TryRemove(spellId, out _);

            if (snapshotsRemoved)
            {
                _logger.LogInfo($"[EncounterSnapshotManager] Cleared {removedSnapshots.Length} snapshots for spell {spellId}");
            }
            else
            {
                _logger.LogInfo($"[EncounterSnapshotManager] No snapshots to clear for spell {spellId}");
            }
        }
        public static void ClearExpiredSnapshots(TimeSpan maxAge)
        {
            var cutoffTime = DateTime.UtcNow - maxAge;
            var spellsToRemove = new List<string>();
            var totalClearedSnapshots = 0;

            foreach (var kvp in _snapshots)
            {
                var spellId = kvp.Key;
                var snapshots = kvp.Value;

                // Check if any snapshots in this spell are still valid
                var validSnapshots = snapshots.Where(s => s.CreatedAt >= cutoffTime).ToImmutableArray();

                if (validSnapshots.IsEmpty)
                {
                    // All snapshots are expired, remove the entire spell
                    spellsToRemove.Add(spellId);
                    totalClearedSnapshots += snapshots.Length;
                }
                else if (validSnapshots.Length < snapshots.Length)
                {
                    // Some snapshots are expired, update with only valid ones
                    _snapshots.TryUpdate(spellId, validSnapshots, snapshots);
                    totalClearedSnapshots += snapshots.Length - validSnapshots.Length;
                }
            }

            // Remove spells with no valid snapshots
            foreach (var spellId in spellsToRemove)
            {
                _snapshots.TryRemove(spellId, out _);
                _actionLookupCache.TryRemove(spellId, out _);
            }

            if (totalClearedSnapshots > 0)
            {
                _logger.LogInfo($"[EncounterSnapshotManager] Cleared {totalClearedSnapshots} expired snapshots " +
                         $"(older than {maxAge.TotalMinutes:F1} minutes)");
            }
        }

        public static int GetTotalSnapshotCount()
        {
            return _snapshots.Values.Sum(snapshots => snapshots.Length);
        }

        public static int GetSpellCount()
        {
            return _snapshots.Count;
        }

        public static void ClearAllSnapshots()
        {
            var totalCleared = GetTotalSnapshotCount();
            _snapshots.Clear();
            _actionLookupCache.Clear();
            
            if (totalCleared > 0)
            {
                _logger.LogInfo($"[EncounterSnapshotManager] Cleared all {totalCleared} snapshots from memory");
            }
        }

        public static void AutoCleanup()
        {
            // Default retention: keep snapshots for 5 minutes
            var defaultRetention = TimeSpan.FromMinutes(5);
            ClearExpiredSnapshots(defaultRetention);
        }

        public static void StoreSnapshotsOptimized(
            string spellId,
            ImmutableArray<EncounterStateSnapshot> snapshots)
        {
            if (string.IsNullOrEmpty(spellId))
                throw new ArgumentException("Spell ID cannot be null or empty", nameof(spellId));
            
            if (snapshots.IsDefault)
                snapshots = ImmutableArray<EncounterStateSnapshot>.Empty;

            // Store snapshots with optimized structure for faster retrieval
            _snapshots.AddOrUpdate(spellId, snapshots, (key, existing) => snapshots);
            
            _logger.LogInfo($"[EncounterSnapshotManager] Stored {snapshots.Length} snapshots for spell {spellId} (optimized)");
        }

        public static (int totalSnapshots, int spellCount, long estimatedMemoryBytes) GetMemoryStats()
        {
            var totalSnapshots = GetTotalSnapshotCount();
            var spellCount = GetSpellCount();
            
            // Rough estimate: each snapshot ~1KB (this is a conservative estimate)
            var estimatedMemoryBytes = totalSnapshots * 1024L;
            
            return (totalSnapshots, spellCount, estimatedMemoryBytes);
        }
    }
}