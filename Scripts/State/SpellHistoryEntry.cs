using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Godot;
using Maximagus.Scripts.Spells.Abstractions;

namespace Scripts.State
{

    public class SpellHistoryEntry
    {
        public DateTime CompletedAt { get; }
        public float TotalDamage { get; }
        public ImmutableDictionary<string, Variant> FinalProperties { get; }
        public ImmutableArray<string> CastCardIds { get; }
        public ImmutableArray<SpellCardResource> CastCardResources { get; }
        public bool WasSuccessful { get; }
        public string ErrorMessage { get; }

        public SpellHistoryEntry(
            DateTime completedAt,
            float totalDamage,
            ImmutableDictionary<string, Variant> finalProperties = null,
            ImmutableArray<string> castCardIds = default,
            ImmutableArray<SpellCardResource> castCardResources = default,
            bool wasSuccessful = true,
            string errorMessage = null)
        {
            CompletedAt = completedAt;
            TotalDamage = totalDamage;
            FinalProperties = finalProperties ?? ImmutableDictionary<string, Variant>.Empty;
            CastCardIds = castCardIds.IsDefault ? ImmutableArray<string>.Empty : castCardIds;
            CastCardResources = castCardResources.IsDefault ? ImmutableArray<SpellCardResource>.Empty : castCardResources;
            WasSuccessful = wasSuccessful;
            ErrorMessage = errorMessage ?? string.Empty;
        }

        public static SpellHistoryEntry CreateSuccessful(
            SpellState currentSpellState,
            ImmutableArray<string> cardIds,
            ImmutableArray<SpellCardResource> cardResources)
        {
            if (currentSpellState == null)
                throw new ArgumentNullException(nameof(currentSpellState));

            return new SpellHistoryEntry(
                DateTime.UtcNow,
                currentSpellState.TotalDamageDealt,
                currentSpellState.Properties,
                cardIds,
                cardResources,
                true,
                null);
        }

        public static SpellHistoryEntry CreateFailed(
            SpellState currentSpellState,
            ImmutableArray<string> cardIds,
            ImmutableArray<SpellCardResource> cardResources,
            string errorMessage)
        {
            if (currentSpellState == null)
                throw new ArgumentNullException(nameof(currentSpellState));

            return new SpellHistoryEntry(
                DateTime.UtcNow,
                currentSpellState.TotalDamageDealt,
                currentSpellState.Properties,
                cardIds,
                cardResources,
                false,
                errorMessage ?? "Unknown error");
        }

        public T GetProperty<[MustBeVariant] T>(string key, T defaultValue)
        {
            return FinalProperties.TryGetValue(key, out var value) ? value.As<T>() : defaultValue;
        }

        public TimeSpan? GetDuration(DateTime? startTime)
        {
            return startTime.HasValue ? CompletedAt - startTime.Value : null;
        }

        public bool IsValid()
        {
            try
            {
                // Completed time should be reasonable
                if (CompletedAt > DateTime.UtcNow.AddMinutes(1))
                    return false;

                // Total damage should not be negative
                if (TotalDamage < 0)
                    return false;

                // Card arrays should have matching lengths if both are present
                if (!CastCardIds.IsEmpty && !CastCardResources.IsEmpty)
                {
                    if (CastCardIds.Length != CastCardResources.Length)
                        return false;
                }

                // Failed spells should have error messages
                if (!WasSuccessful && string.IsNullOrEmpty(ErrorMessage))
                    return false;

                // Successful spells should not have error messages
                if (WasSuccessful && !string.IsNullOrEmpty(ErrorMessage))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is SpellHistoryEntry other)
            {
                return CompletedAt == other.CompletedAt &&
                       Math.Abs(TotalDamage - other.TotalDamage) < 0.001f &&
                       PropertiesEqual(FinalProperties, other.FinalProperties) &&
                       CastCardIds.SequenceEqual(other.CastCardIds) &&
                       CastCardResources.SequenceEqual(other.CastCardResources) &&
                       WasSuccessful == other.WasSuccessful &&
                       ErrorMessage == other.ErrorMessage;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                CompletedAt,
                TotalDamage,
                FinalProperties.Count,
                CastCardIds.Length,
                CastCardResources.Length,
                WasSuccessful,
                ErrorMessage);
        }

        public override string ToString()
        {
            var status = WasSuccessful ? "Success" : $"Failed: {ErrorMessage}";
            var cardCount = Math.Max(CastCardIds.Length, CastCardResources.Length);
            return $"SpellHistoryEntry[{CompletedAt:HH:mm:ss}, {TotalDamage:F1} damage, " +
                   $"{cardCount} cards, {status}]";
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