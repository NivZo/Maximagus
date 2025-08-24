
using System;
using Godot;
using Godot.Collections;
using Maximagus.Scripts.Enums;
using Scripts.State;
using Scripts.Commands;
using Scripts.Commands.Spell;
using Scripts.Utilities;
using System.Linq;

namespace Maximagus.Resources.Definitions.Actions
{
    [GlobalClass]
    public partial class ModifierActionResource : ActionResource
    {
        [Export] public bool IsConsumedOnUse { get; set; }
        [Export] public Array<SpellModifierCondition> SpellModifierConditions { get; set; }
        [Export] public ModifierType ModifierType { get; set; }
        [Export] public DamageType Element { get; set; }
        [Export] public float Value { get; set; }

        public ModifierActionResource()
        {
            ResourceLocalToScene = true;
            SpellModifierConditions = [];
        }

        public override string GetPopUpEffectText(IGameStateData gameState)
        {
            return ModifierType switch
            {
                ModifierType.Add => $"+{Value}",
                ModifierType.Multiply => $"x{Value}",
                ModifierType.Set => $"={Value}",
                _ => string.Empty
            };
        }

        public override Color PopUpEffectColor => Element switch
        {
            DamageType.Fire => new Color(1, 0.5f, 0),
            DamageType.Frost => new Color(0, 0.5f, 1),
            DamageType.PerChill => new Color(0, 0.5f, 1),
            _ => new Color(1, 1, 1)
        };

        public override GameCommand CreateExecutionCommand(string cardId)
        {
            return new AddSpellModifierCommand(
                ModifierType,
                Element,
                Value,
                IsConsumedOnUse,
                SpellModifierConditions?.ToArray() ?? System.Array.Empty<SpellModifierCondition>());
        }

        public bool CanApply(DamageActionResource damageAction)
        {
            var canApply = true;

            foreach (var condition in SpellModifierConditions)
            {
                if (canApply)
                {
                    canApply = canApply && condition switch
                    {
                        SpellModifierCondition.IsFire => damageAction.DamageType == DamageType.Fire,
                        SpellModifierCondition.IsFrost => damageAction.DamageType == DamageType.Frost,
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
                ModifierType.Add => baseDamage + Value,
                ModifierType.Multiply => baseDamage * Value,
                ModifierType.Set => Value,
                _ => baseDamage,
            };
            GD.Print($"Applying modifier - from base {baseDamage} to {modifiedDamage}");
            return modifiedDamage;
        }
    }
}
