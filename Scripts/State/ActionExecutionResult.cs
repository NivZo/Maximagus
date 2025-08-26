using System;
using System.Collections.Immutable;
using Maximagus.Resources.Definitions.Actions;

namespace Scripts.State
{

    public class ActionExecutionResult
    {
        public ActionResource Action { get; }
        public float FinalDamage { get; }
        public ImmutableArray<ModifierData> ConsumedModifiers { get; }
        public ImmutableArray<ModifierData> RemainingModifiers { get; }
        public DateTime CalculatedAt { get; }

        public ActionExecutionResult(
            ActionResource action,
            float finalDamage,
            ImmutableArray<ModifierData> consumedModifiers,
            ImmutableArray<ModifierData> remainingModifiers,
            DateTime calculatedAt)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            FinalDamage = finalDamage;
            ConsumedModifiers = consumedModifiers.IsDefault ? ImmutableArray<ModifierData>.Empty : consumedModifiers;
            RemainingModifiers = remainingModifiers.IsDefault ? ImmutableArray<ModifierData>.Empty : remainingModifiers;
            CalculatedAt = calculatedAt;
        }

        public static ActionExecutionResult Create(
            ActionResource action,
            float finalDamageDealt = 0,
            ImmutableArray<ModifierData> consumedModifiers = default,
            ImmutableArray<ModifierData> remainingModifiers = default)
        {
            return new ActionExecutionResult(
                action,
                finalDamageDealt,
                consumedModifiers.IsDefaultOrEmpty ? ImmutableArray<ModifierData>.Empty : consumedModifiers,
                remainingModifiers.IsDefaultOrEmpty ? ImmutableArray<ModifierData>.Empty : remainingModifiers,
                DateTime.UtcNow);
        }

        public bool IsValid()
        {
            try
            {
                // Action should not be null
                if (Action == null)
                    return false;

                // Final damage should not be negative
                if (FinalDamage < 0)
                    return false;

                // Calculated time should be reasonable
                if (CalculatedAt > DateTime.UtcNow.AddMinutes(1))
                    return false;

                // Validate all consumed modifiers
                foreach (var modifier in ConsumedModifiers)
                {
                    if (!modifier.IsValid())
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
            if (obj is ActionExecutionResult other)
            {
                return Action.Equals(other.Action) &&
                       Math.Abs(FinalDamage - other.FinalDamage) < 0.001f &&
                       ConsumedModifiers.Equals(other.ConsumedModifiers) &&
                       RemainingModifiers.Equals(other.RemainingModifiers) &&
                       CalculatedAt == other.CalculatedAt;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Action, FinalDamage, ConsumedModifiers.Length, RemainingModifiers.Length, CalculatedAt);
        }

        public override string ToString()
        {
            return $"ActionExecutionResult[{Action.GetType().Name}, Damage: {FinalDamage:F1}, " +
                   $"Consumed: {ConsumedModifiers.Length}, Remaining: {RemainingModifiers.Length}, At: {CalculatedAt:HH:mm:ss}]";
        }
    }
}