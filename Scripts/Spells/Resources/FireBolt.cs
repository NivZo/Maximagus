
using Godot;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Scripts.Spells.Resources
{
    [GlobalClass]
    public partial class FireBolt : ActionCardResource
    {
        [Export] public float Damage { get; set; } = 10f;

        public override void Execute(SpellContext context)
        {
            var finalDamage = context.ApplyDamageModifiers(Damage, DamageType.Fire);
            // Apply damage to target
            GD.Print($"Dealt {finalDamage} fire damage.");

            context.ModifyProperty(ContextProperty.FireDamageDealt, finalDamage, ModifierType.Add);
            var fireInstances = context.GetProperty<int>(ContextProperty.FireInstances.ToString(), 0);
            context.SetProperty(ContextProperty.FireInstances.ToString(), fireInstances + 1);
        }

        public override bool CanInteractWith(SpellContext context)
        {
            return true;
        }
    }
}
