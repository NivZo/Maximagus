
using Godot;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Scripts.Spells.Resources
{
    [GlobalClass]
    public abstract partial class SpellModifierResource : SpellCardResource
    {
        public override void Execute(SpellContext context)
        {
            context.AddModifier(this);
        }

        public override bool CanInteractWith(SpellContext context)
        {
            return true;
        }
    }
}
