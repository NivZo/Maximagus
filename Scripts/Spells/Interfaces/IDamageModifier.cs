namespace Maximagus.Scripts.Spells.Interfaces
{
    public interface IDamageModifier
    {
        bool IsConsumedOnUse { get; }
        bool CanApply(DamageType damageType);
        float Apply(float baseDamage);
    }
}
