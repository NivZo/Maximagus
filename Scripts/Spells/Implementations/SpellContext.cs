
using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Interfaces;

namespace Maximagus.Scripts.Spells.Implementations
{
    public partial class SpellContext : RefCounted
    {
        public Dictionary<string, Variant> Properties { get; set; } = new();
        public Array<Resource> ActiveModifiers { get; set; } = new();
        public Array<Resource> QueuedEffects { get; set; } = new();

        public T GetProperty<[MustBeVariant] T>(string key, T defaultValue)
        {
            return Properties.TryGetValue(key, out var value) ? value.As<T>() : defaultValue;
        }

        public void SetProperty<[MustBeVariant] T>(string key, T value)
        {
            Properties[key] = Variant.From(value);
        }

        public void ModifyProperty(ContextProperty key, float value, ModifierType type)
        {
            var currentValue = GetProperty<float>(key.ToString(), 0f);
            Properties[key.ToString()] = type switch
            {
                ModifierType.Add => currentValue + value,
                ModifierType.Multiply => currentValue * value,
                _ => currentValue
            };
        }

        public void AddModifier(Resource modifier)
        {
            ActiveModifiers.Add(modifier);
        }

        public float ApplyDamageModifiers(float baseDamage, DamageType damageType)
        {
            float modifiedDamage = baseDamage;
            var modifiersToRemove = new Array<Resource>();

            foreach (var modifier in ActiveModifiers)
            {
                if (modifier is IDamageModifier damageModifier && damageModifier.CanApply(damageType))
                {
                    modifiedDamage = damageModifier.Apply(modifiedDamage);
                    GD.Print($"Applied a damage modifier, new damage: {modifiedDamage}");

                    if (damageModifier.IsConsumedOnUse)
                    {
                        modifiersToRemove.Add(modifier);
                    }
                }
            }

            foreach (var modifier in modifiersToRemove)
            {
                ActiveModifiers.Remove(modifier);
            }

            return modifiedDamage;
        }
    }

    public enum ModifierType
    {
        Add,
        Multiply,
        Set,
    }
}
