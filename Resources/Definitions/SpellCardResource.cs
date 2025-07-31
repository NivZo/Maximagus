using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Implementations;

namespace Maximagus.Scripts.Spells.Abstractions
{
    [GlobalClass]
    public abstract partial class SpellCardResource : Resource
    {
        [Export] public string CardId { get; set; }
        [Export] public string CardName { get; set; }
        [Export] public Texture2D CardArt { get; set; }

        [ExportGroup("Spell Action")]
        [Export] public CardType CardType { get; set; }
        [Export] public DamageType DamageType { get; set; }
        [Export] public int ActionValue { get; set; }



        public abstract void Execute(SpellContext context);
    }
}