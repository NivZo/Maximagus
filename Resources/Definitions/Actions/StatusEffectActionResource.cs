using Godot;
using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Resources.Definitions.Actions
{
    [GlobalClass]
    public partial class StatusEffectActionResource : ActionResource
    {
        [Export] public StatusEffectResource StatusEffect { get; set; }
        [Export] public int Stacks { get; set; } = 1;

        public override void Execute(SpellContext context)
        {
            GD.Print($"Applying status effect: {StatusEffect.EffectName}");
            var statusManager = ServiceLocator.GetService<IStatusEffectManager>();
            statusManager?.AddStatusEffect(StatusEffect, Stacks);
        }
    }
}
