namespace Maximagus.Scripts.Spells.Abstractions
{
    public interface IDamageModifier
    {
        bool IsConsumedOnUse { get; }
        bool CanApply(DamageType damageType);
        float Apply(float baseDamage);
    }
}
