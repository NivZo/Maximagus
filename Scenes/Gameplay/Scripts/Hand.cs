using System.Linq;
using Godot;
using Scripts.State;

public partial class Hand : CardContainer
{
    public override void _Ready()
    {
        base._Ready();
    }

    public override CardState[] GetCardStates(IGameStateData currentState)
    {
        return currentState.Cards.HandCards.ToArray();
    }

    public override void OnCardEnter(Card card)
    {
        card.GlobalPosition = GetViewportRect().Size + new Vector2(card.Size.X * 2, -card.Size.Y * 3);
    }
}
