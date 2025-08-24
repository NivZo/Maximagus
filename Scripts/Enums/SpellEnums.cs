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
        PerChill,
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

    public enum StatusEffectActionType
    {
        Add,
        Set,
        Remove,
    }

    public enum SpellModifierCondition
    {
        IsFire,
        IsFrost,
    }
}