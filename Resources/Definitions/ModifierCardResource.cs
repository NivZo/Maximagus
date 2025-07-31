
using System;
using Godot;
using Godot.Collections;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Resources.Definitions
{
    [GlobalClass]
    public partial class ModifierCardResource : SpellCardResource
    {
        [ExportGroup("Modifier Action")]
        [Export] public bool IsConsumedOnUse { get; set; }
        [Export] public Array<SpellModifierCondition> SpellModifierConditions { get; set; }
        [Export] public ModifierType ModifierType { get; set; }

        public override void Execute(SpellContext context)
        {
            GD.Print($"Adding modifier {CardName}");
            context.AddModifier(this);
        }

        public bool CanApply(SpellCardResource spellCardResource)
        {
            var canApply = true;

            foreach (var condition in SpellModifierConditions)
            {
                if (canApply)
                {
                    canApply = canApply && condition switch
                    {
                        SpellModifierCondition.IsFire => spellCardResource is ActionCardResource actionCardResource && actionCardResource.CardType == CardType.Damage && actionCardResource.DamageType == DamageType.Fire,
                        _ => false,
                    };
                }
            }

            return canApply;
        }

        public float Apply(float baseDamage)
        {
            var modifiedDamage = ModifierType switch
            {
                ModifierType.Add => baseDamage + ActionValue,
                ModifierType.Multiply => baseDamage * ActionValue,
                ModifierType.Set => ActionValue,
                _ => baseDamage,
            };
            GD.Print($"Applying modifier {CardName} - from base {baseDamage} to {modifiedDamage}");
            return modifiedDamage;
        }
    }
}
