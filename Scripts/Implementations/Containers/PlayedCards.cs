using System.Linq;
using Scripts.State;

public partial class PlayedCards : CardContainer
{
	public override void _Ready()
	{
		base._Ready();
	}

	public override CardState[] GetCardStates(IGameStateData currentState)
	{
		return currentState.Cards.PlayedCards.ToArray();
	}
}
