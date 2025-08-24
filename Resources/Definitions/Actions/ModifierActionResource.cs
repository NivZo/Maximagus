
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
            try
            {
                // Get the base modifier text
                var baseText = ModifierType switch
                {
                    ModifierType.Add => $"+{Value}",
                    ModifierType.Multiply => $"x{Value}",
                    ModifierType.Set => $"={Value}",
                    _ => string.Empty
                };
                
                // For consumable modifiers, we could enhance the display to show if it was consumed
                // But for now, modifiers are added to the spell state and the popup shows the modifier value
                // The consumption happens later when damage actions are executed
                
                // Try to get snapshot information to see if this modifier was added successfully
                var snapshot = SnapshotLookupHelper.TryGetSnapshotForAction(gameState, ActionId, "ModifierActionResource");
                
                if (snapshot != null)
                {
                    // We could check if the modifier was successfully added by comparing
                    // the modifier count before and after, but for now just show the base text
                    GD.Print($"[ModifierActionResource] Using snapshot for modifier popup: {baseText} (action: {ActionId})");
                    return baseText;
                }
                
                GD.Print($"[ModifierActionResource] Using base text for modifier popup: {baseText} (action: {ActionId})");
                return baseText;
            }
            catch (Exception ex)
            {
                GD.Print($"[ModifierActionResource] Error getting popup text: {ex.Message}");
                
                // Fallback to simple display
                return ModifierType switch
                {
                    ModifierType.Add => $"+{Value}",
                    ModifierType.Multiply => $"x{Value}",
                    ModifierType.Set => $"={Value}",
                    _ => string.Empty
                };
            }
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
