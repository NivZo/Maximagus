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
        Physical
    }

    public enum ContextProperty
    {
        FireDamageDealt,
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
    }
}