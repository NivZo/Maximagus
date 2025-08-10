using System.Linq;
using Godot;
using Scripts.Commands;
using Scripts.Commands.Hand;
using Scripts.State;

public partial class DiscardedCards : CardContainer
{
	public override void _Ready()
	{
		base._Ready();
	}

	public override CardState[] GetCardStates(IGameStateData currentState)
	{
		return currentState.Hand.Cards.Where(card => card.ContainerType == ContainerType.DiscardedCards).ToArray();
	}

	public override void OnCardEnter(Card card)
	{
		GetTree().CreateTimer(2).Timeout += () => ServiceLocator.GetService<IGameCommandProcessor>().ExecuteCommand(new RemoveCardCommand(card.CardId));
    }
}
