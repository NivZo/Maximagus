namespace Maximagus.Scripts.Enums
{
    public enum CardType
    {
        Damage,
        Modifier,
        Utility
    }

    public enum DamageType
    {
        None,
        Fire,
        Frost,
    }

    public enum ContextProperty
    {
        FireDamageDealt,
        FrostDamageDealt,
    }

    public enum ContextPropertyOperation
    {
        Add,
        Multiply,
        Set
    }

    public enum ModifierType
    {
        Add,
        Multiply,
        Set,
    }

    public enum SpellModifierCondition
    {
        IsFire,
        IsFrost,
    }
}