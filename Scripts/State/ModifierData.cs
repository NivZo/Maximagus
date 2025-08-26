using System;
using System.Collections.Immutable;
using System.Linq;
using Maximagus.Scripts.Enums;

namespace Scripts.State
{

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