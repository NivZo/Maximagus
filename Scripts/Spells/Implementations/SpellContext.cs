
using Godot;
using Godot.Collections;
using Maximagus.Resources.Definitions;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Abstractions;

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

        public void ModifyProperty(ContextProperty key, float value, ContextPropertyOperation operation)
        {
            var currentValue = GetProperty(key.ToString(), 0f);
            Properties[key.ToString()] = operation switch
            {
                ContextPropertyOperation.Add => currentValue + value,
                ContextPropertyOperation.Multiply => currentValue * value,
                ContextPropertyOperation.Set => currentValue,
                _ => currentValue
            };
        }

        public void AddModifier(Resource modifier)
        {
            ActiveModifiers.Add(modifier);
        }

        public float ApplyDamageModifiers(SpellCardResource spellCardResource)
        {
            if (spellCardResource is CombatCardResource actionCardResource && actionCardResource.CardType == CardType.Damage)
            {
                float modifiedDamage = actionCardResource.ActionValue;
                var modifiersToRemove = new Array<Resource>();

                foreach (var modifier in ActiveModifiers)
                {
                    if (modifier is ModifierCardResource damageModifier && damageModifier.CanApply(spellCardResource))
                    {
                        modifiedDamage = damageModifier.Apply(modifiedDamage);
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

            return 0;
        }
    }
}
