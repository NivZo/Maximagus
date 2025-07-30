
using Godot;
using Maximagus.Scripts.Spells.Implementations;
using Maximagus.Scripts.Spells.Interfaces;

namespace Maximagus.Scripts.Spells.Resources
{
    [GlobalClass]
    public partial class DoubleFireModifier : ModifierCardResource, IDamageModifier
    {
        public bool IsConsumedOnUse => true;

        public bool CanApply(DamageType damageType) => damageType == DamageType.Fire;

        public float Apply(float baseDamage)
        {
            return baseDamage * 2;
        }

        public override void Execute(SpellContext context)
        {
            context.AddModifier(this);
            GD.Print("Doubling next fire damage.");
        }

        public override bool CanInteractWith(SpellContext context)
        {
            return true;
        }
    }
}
