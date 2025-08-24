using System;
using System.Collections.Immutable;
using System.Linq;
using Maximagus.Scripts.Enums;

namespace Scripts.State
{
    /// <summary>
    /// Immutable data representing a spell modifier in state
    /// </summary>
    public class ModifierData
    {
        public ModifierType Type { get; }
        public DamageType Element { get; }
        public float Value { get; }
        public bool IsConsumedOnUse { get; }
        public ImmutableArray<SpellModifierCondition> Conditions { get; }

        public ModifierData(
            ModifierType type,
            DamageType element,
            float value,
            bool isConsumedOnUse,
            ImmutableArray<SpellModifierCondition> conditions = default)
        {
            Type = type;
            Element = element;
            Value = value;
            IsConsumedOnUse = isConsumedOnUse;
            Conditions = conditions.IsDefault ? ImmutableArray<SpellModifierCondition>.Empty : conditions;
        }

        /// <summary>
        /// Creates a ModifierData from a ModifierActionResource
        /// </summary>
        public static ModifierData FromActionResource(Maximagus.Resources.Definitions.Actions.ModifierActionResource resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            var conditions = resource.SpellModifierConditions?.ToImmutableArray() ?? ImmutableArray<SpellModifierCondition>.Empty;

            return new ModifierData(
                resource.ModifierType,
                resource.Element,
                resource.Value,
                resource.IsConsumedOnUse,
                conditions);
        }

        /// <summary>
        /// Determines if this modifier can be applied to a damage action
        /// </summary>
        public bool CanApply(DamageType damageType)
        {
            if (Conditions.IsEmpty)
                return true;

            foreach (var condition in Conditions)
            {
                var conditionMet = condition switch
                {
                    SpellModifierCondition.IsFire => damageType == DamageType.Fire,
                    SpellModifierCondition.IsFrost => damageType == DamageType.Frost,
                    _ => false
                };

                if (!conditionMet)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Applies this modifier to a base damage value
        /// </summary>
        public float Apply(float baseDamage)
        {
            return Type switch
            {
                ModifierType.Add => baseDamage + Value,
                ModifierType.Multiply => baseDamage * Value,
                ModifierType.Set => Value,
                _ => baseDamage
            };
        }

        /// <summary>
        /// Validates that the modifier data is consistent
        /// </summary>
        public bool IsValid()
        {
            try
            {
                // Value should be reasonable for the modifier type
                switch (Type)
                {
                    case ModifierType.Add:
                        // Add modifiers can be any value
                        break;
                    case ModifierType.Multiply:
                        // Multiply modifiers should be positive
                        if (Value <= 0)
                            return false;
                        break;
                    case ModifierType.Set:
                        // Set modifiers should be non-negative
                        if (Value < 0)
                            return false;
                        break;
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
            if (obj is ModifierData other)
            {
                return Type == other.Type &&
                       Element == other.Element &&
                       Math.Abs(Value - other.Value) < 0.001f &&
                       IsConsumedOnUse == other.IsConsumedOnUse &&
                       Conditions.SequenceEqual(other.Conditions);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Element, Value, IsConsumedOnUse, Conditions.Length);
        }

        public override string ToString()
        {
            var conditionsStr = Conditions.IsEmpty ? "None" : string.Join(", ", Conditions);
            return $"ModifierData[{Type} {Value} {Element}, Consumed: {IsConsumedOnUse}, Conditions: {conditionsStr}]";
        }
    }
}