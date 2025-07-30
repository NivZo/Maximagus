
using Godot;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Scripts.Spells.Resources
{
    public enum CardType
    {
        Action,
        Modifier,
        Utility
    }

    [GlobalClass]
    public abstract partial class SpellCardResource : Resource
    {
        [Export] public string CardId { get; set; }
        [Export] public string CardName { get; set; }
        [Export] public CardType CardType { get; set; }
        [Export] public int ExecutionPriority { get; set; }

        public abstract void Execute(SpellContext context);
        public abstract bool CanInteractWith(SpellContext context);
        public virtual int GetExecutionPriority() => ExecutionPriority;
    }
}
