using Godot;
using Maximagus.Scripts.Spells.Resources;

public partial class CardResource : Resource
{
    [Export]
    public SpellCardResource SpellCard { get; set; }

    public int Value;
}