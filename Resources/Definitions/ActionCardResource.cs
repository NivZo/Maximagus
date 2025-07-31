
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Resources.Definitions
{
    [GlobalClass]
    public partial class ActionCardResource : SpellCardResource
    {
        public override void Execute(SpellContext context)
        {
            switch (CardType)
            {
                case CardType.Damage:
                    DealDamage(context);
                    break;
                case CardType.Utility:
                    break;
                case CardType.Modifier:
                    break;
            }
        }

        private void DealDamage(SpellContext context)
        {
            var finalDamage = context.ApplyDamageModifiers(this);
            // Apply damage to target
            GD.Print($"Dealt {finalDamage} damage of type {DamageType}.");

            if (finalDamage > 0)
            {
                var damageDealtContextProperty = DamageType switch
                {
                    DamageType.Fire => ContextProperty.FireDamageDealt,
                    _ => throw new System.Exception($"No context property implemented for damage type {DamageType}")
                };
            
                context.ModifyProperty(damageDealtContextProperty, finalDamage, ContextPropertyOperation.Add);
            }

        }

    }
}
