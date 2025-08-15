
using Godot;
using Godot.Collections;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Enums;

namespace Maximagus.Scripts.Spells.Implementations
{
    public partial class SpellContext : RefCounted
    {
        public Dictionary<string, Variant> Properties { get; set; } = new();
        public Array<ModifierActionResource> ActiveModifiers { get; set; } = new();
        public Array<Resource> QueuedEffects { get; set; } = new();
        public float TotalDamageDealt = 0;

        public T GetProperty<[MustBeVariant] T>(string key, T defaultValue)
        {
            return Properties.TryGetValue(key, out var value) ? value.As<T>() : defaultValue;
        }

        public void SetProperty<[MustBeVariant] T>(string key, T value)
        {
            Properties[key] = Variant.From(value);
        }

        public void ModifyProperty(ContextProperty key, float value, ContextPropertyOperation operation)
        {
            var currentValue = GetProperty(key.ToString(), 0f);
            Properties[key.ToString()] = operation switch
            {
                ContextPropertyOperation.Add => currentValue + value,
                ContextPropertyOperation.Multiply => currentValue * value,
                ContextPropertyOperation.Set => value,
                _ => currentValue
            };
        }

        public void AddModifier(ModifierActionResource modifier)
        {
            ActiveModifiers.Add(modifier);
        }

        public float ApplyDamageModifiersWithoutConsuming(DamageActionResource damageAction)
        {
            float modifiedDamage = damageAction.GetRawDamage();

            foreach (var modifier in ActiveModifiers)
            {
                if (modifier.CanApply(damageAction))
                {
                    modifiedDamage = modifier.Apply(modifiedDamage);
                }
            }

            return modifiedDamage;
        }

        public float ApplyDamageModifiers(DamageActionResource damageAction)
        {
            float modifiedDamage = damageAction.GetRawDamage();
            var modifiersToRemove = new Array<ModifierActionResource>();

            foreach (var modifier in ActiveModifiers)
            {
                if (modifier.CanApply(damageAction))
                {
                    modifiedDamage = modifier.Apply(modifiedDamage);
                    if (modifier.IsConsumedOnUse)
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
}
