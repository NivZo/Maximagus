using System;
using System.Linq;
using Scripts.State;
using Scripts.Config;
using Scripts.Commands;
using Maximagus.Scripts.Managers;
using Scripts.Commands.Hand;

namespace Scripts.Commands.Game
{
    /// <summary>
    /// PURE COMMAND: Handles turn start - draws cards to max hand size and transitions to CardSelection
    /// Natural phase flow: TurnEnd -> TurnStart (processing) -> CardSelection
    /// </summary>
    public class TurnEndCommand : GameCommand
    {

        public TurnEndCommand() : base()
        {
        }

        public override string GetDescription() => "Start Turn";

        public override bool CanExecute()
        {
            return _commandProcessor.CurrentState.Phase.CurrentPhase == GamePhase.TurnEnd;
        }


        public override void Execute(CommandCompletionToken token)
        {
            // First transition to TurnStart phase (intermediate phase for processing)
            var currentState = _commandProcessor.CurrentState;
            // Remove the selected cards from hand
            var playedCards = currentState.Hand.Cards.Where(c => c.ContainerType == ContainerType.PlayedCards).Select(c => c.CardId);
            var newHandState = currentState.Hand.WithRemovedCards(playedCards);

            // Transition to CardSelection phase (natural end state)
            var newPhaseState = _commandProcessor.CurrentState.Phase.WithPhase(GamePhase.TurnStart);
            var finalState = _commandProcessor.CurrentState
                .WithHand(newHandState)
                .WithPhase(newPhaseState);

            _logger?.LogInfo($"[TurnEndCommand] Turn ended - cards played: {playedCards.Count()}, final phase: {finalState.Phase.CurrentPhase}");

            token.Complete(CommandResult.Success(finalState, [ new TurnStartCommand() ]));
        }
    }
}