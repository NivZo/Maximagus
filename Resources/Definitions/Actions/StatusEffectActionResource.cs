using Godot;
using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Resources.Definitions.Actions
{
    [GlobalClass]
    public partial class StatusEffectActionResource : ActionResource
    {
        [Export] public StatusEffectResource StatusEffect { get; set; }
        [Export] public StatusEffectActionType ActionType { get; set; }
        [Export] public int Stacks { get; set; } = 1;

        public override string GetPopUpEffectText(SpellContext context) => ActionType switch
        {
            StatusEffectActionType.Add => $"+{Stacks} {StatusEffect.EffectType}",
            StatusEffectActionType.Multiply => $"x{Stacks} {StatusEffect.EffectType}",
            StatusEffectActionType.Set => $"-{StatusEffect.Value} {StatusEffect.EffectType}",
            _ => string.Empty
        };
        public override Color PopUpEffectColor => StatusEffect.EffectType switch
        {
            StatusEffectType.Poison => new Color(1, 0.5f, 0),
            StatusEffectType.Chill => new Color(0, 0.5f, 1),
            StatusEffectType.Burning => new Color(0, 0.5f, 1),
            StatusEffectType.Bleeding => new Color(0, 0.5f, 1),
            _ => new Color(1, 1, 1)
        };

        public override void Execute(SpellContext context)
        {
            GD.Print($"Applying status effect: {StatusEffect.EffectName}");
            var statusManager = ServiceLocator.GetService<IStatusEffectManager>();
            statusManager.AddStatusEffect(StatusEffect, Stacks, ActionType);
        }
    }
}
