
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Resources.Definitions.Actions
{
    [GlobalClass]
    public partial class DamageActionResource : ActionResource
    {
        [Export] public DamageType DamageType { get; set; }
        [Export] public int Amount { get; set; }

        public override void Execute(SpellContext context)
        {
            var finalDamage = context.ApplyDamageModifiers(this);

            GD.Print($"Dealt {finalDamage} damage of type {DamageType}.");
            context.TotalDamageDealt += finalDamage;

            if (finalDamage > 0)
            {
                var damageDealtContextProperty = DamageType switch
                {
                    DamageType.Fire => ContextProperty.FireDamageDealt,
                    DamageType.Frost => ContextProperty.FrostDamageDealt,
                    DamageType.PerChill => ContextProperty.FrostDamageDealt,
                    _ => throw new System.Exception($"No context property implemented for damage type {DamageType}")
                };

                context.ModifyProperty(damageDealtContextProperty, finalDamage, ContextPropertyOperation.Add);
            }
        }

        public int GetRawDamage()
        {
            return DamageType switch
            {
                DamageType.None => Amount,
                DamageType.Fire => Amount,
                DamageType.Frost => Amount,
                DamageType.PerChill => Amount * ServiceLocator.GetService<IStatusEffectManager>().GetStacksOfEffect(StatusEffectType.Chill),
                _ => Amount,
            };
        }
    }
}
