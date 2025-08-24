using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.Actions;

namespace Scripts.State
{
    /// <summary>
    /// Immutable state representing the current spell being cast and spell history
    /// </summary>
    public class SpellState
    {
        public bool IsActive { get; }
        public ImmutableDictionary<string, Variant> Properties { get; }
        public ImmutableArray<ModifierData> ActiveModifiers { get; }
        public float TotalDamageDealt { get; }
        public ImmutableArray<SpellHistoryEntry> History { get; }
        public DateTime? StartTime { get; }
        public int CurrentActionIndex { get; }


        public SpellState(
            bool isActive = false,
            ImmutableDictionary<string, Variant> properties = null,
            ImmutableArray<ModifierData> activeModifiers = default,
            float totalDamageDealt = 0f,
            ImmutableArray<SpellHistoryEntry> history = default,
            DateTime? startTime = null,
            int currentActionIndex = 0)
        {
            IsActive = isActive;
            Properties = properties ?? ImmutableDictionary<string, Variant>.Empty;
            ActiveModifiers = activeModifiers.IsDefault ? ImmutableArray<ModifierData>.Empty : activeModifiers;
            TotalDamageDealt = totalDamageDealt;
            History = history.IsDefault ? ImmutableArray<SpellHistoryEntry>.Empty : history;
            StartTime = startTime;
            CurrentActionIndex = currentActionIndex;
        }

        /// <summary>
        /// Creates an initial empty spell state
        /// </summary>
        public static SpellState CreateInitial()
        {
            return new SpellState();
        }

        /// <summary>
        /// Creates a new spell state with an active spell
        /// </summary>
        public SpellState WithActiveSpell(DateTime startTime)
        {
            return new SpellState(
                isActive: true,
                properties: ImmutableDictionary<string, Variant>.Empty,
                activeModifiers: ImmutableArray<ModifierData>.Empty,
                totalDamageDealt: 0f,
                history: History,
                startTime: startTime,
                currentActionIndex: 0);
        }

        /// <summary>
        /// Creates a new spell state with updated properties
        /// </summary>
        public SpellState WithProperties(ImmutableDictionary<string, Variant> newProperties)
        {
            return new SpellState(
                IsActive,
                newProperties,
                ActiveModifiers,
                TotalDamageDealt,
                History,
                StartTime,
                CurrentActionIndex);
        }

        /// <summary>
        /// Creates a new spell state with an added property
        /// </summary>
        public SpellState WithProperty(string key, Variant value)
        {
            return WithProperties(Properties.SetItem(key, value));
        }

        /// <summary>
        /// Creates a new spell state with a removed property
        /// </summary>
        public SpellState WithoutProperty(string key)
        {
            return WithProperties(Properties.Remove(key));
        }

        /// <summary>
        /// Creates a new spell state with updated modifiers
        /// </summary>
        public SpellState WithModifiers(ImmutableArray<ModifierData> newModifiers)
        {
            return new SpellState(
                IsActive,
                Properties,
                newModifiers,
                TotalDamageDealt,
                History,
                StartTime,
                CurrentActionIndex);
        }

        /// <summary>
        /// Creates a new spell state with an added modifier
        /// </summary>
        public SpellState WithAddedModifier(ModifierData modifier)
        {
            return WithModifiers(ActiveModifiers.Add(modifier));
        }

        /// <summary>
        /// Creates a new spell state with updated total damage
        /// </summary>
        public SpellState WithTotalDamage(float newTotalDamage)
        {
            return new SpellState(
                IsActive,
                Properties,
                ActiveModifiers,
                newTotalDamage,
                History,
                StartTime,
                CurrentActionIndex);
        }

        /// <summary>
        /// Creates a new spell state with updated action index
        /// </summary>
        public SpellState WithActionIndex(int newActionIndex)
        {
            return new SpellState(
                IsActive,
                Properties,
                ActiveModifiers,
                TotalDamageDealt,
                History,
                StartTime,
                newActionIndex);
        }

        /// <summary>
        /// Creates a new spell state with the current spell moved to history
        /// </summary>
        public SpellState WithCompletedSpell(SpellHistoryEntry historyEntry)
        {
            var newHistory = History.Add(historyEntry);
            
            // Keep only the last 50 spell history entries to prevent unbounded growth
            if (newHistory.Length > 50)
            {
                newHistory = newHistory.RemoveRange(0, newHistory.Length - 50);
            }

            return new SpellState(
                isActive: false,
                properties: ImmutableDictionary<string, Variant>.Empty,
                activeModifiers: ImmutableArray<ModifierData>.Empty,
                totalDamageDealt: 0f,
                history: newHistory,
                startTime: null,
                currentActionIndex: 0);
        }

        /// <summary>
        /// Gets a property value with a default fallback
        /// </summary>
        public T GetProperty<[MustBeVariant] T>(string key, T defaultValue)
        {
            return Properties.TryGetValue(key, out var value) ? value.As<T>() : defaultValue;
        }



        /// <summary>
        /// Validates that the spell state is consistent
        /// </summary>
        public bool IsValid()
        {
            try
            {
                // Active spell must have start time
                if (IsActive && !StartTime.HasValue)
                    return false;

                // Inactive spell should not have start time
                if (!IsActive && StartTime.HasValue)
                    return false;

                // Total damage should not be negative
                if (TotalDamageDealt < 0)
                    return false;

                // Current action index should not be negative
                if (CurrentActionIndex < 0)
                    return false;

                // Validate all modifiers
                foreach (var modifier in ActiveModifiers)
                {
                    if (!modifier.IsValid())
                        return false;
                }

                // Validate all history entries
                foreach (var entry in History)
                {
                    if (!entry.IsValid())
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is SpellState other)
            {
                return IsActive == other.IsActive &&
                       PropertiesEqual(Properties, other.Properties) &&
                       ActiveModifiers.SequenceEqual(other.ActiveModifiers) &&
                       Math.Abs(TotalDamageDealt - other.TotalDamageDealt) < 0.001f &&
                       History.SequenceEqual(other.History) &&
                       StartTime == other.StartTime &&
                       CurrentActionIndex == other.CurrentActionIndex;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                IsActive,
                Properties.Count,
                ActiveModifiers.Length,
                TotalDamageDealt,
                History.Length,
                StartTime,
                CurrentActionIndex);
        }

        public override string ToString()
        {
            return $"SpellState[Active: {IsActive}, Modifiers: {ActiveModifiers.Length}, " +
                   $"Damage: {TotalDamageDealt:F1}, History: {History.Length}, " +
                   $"ActionIndex: {CurrentActionIndex}]";
        }

        private static bool PropertiesEqual(ImmutableDictionary<string, Variant> dict1, ImmutableDictionary<string, Variant> dict2)
        {
            if (dict1.Count != dict2.Count)
                return false;

            foreach (var kvp in dict1)
            {
                if (!dict2.TryGetValue(kvp.Key, out var value) || !kvp.Value.Equals(value))
                    return false;
            }

            return true;
        }


    }
}