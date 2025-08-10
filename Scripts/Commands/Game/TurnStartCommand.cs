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
    public class TurnStartCommand : GameCommand
    {
        private readonly IHandManager _handManager;

        public TurnStartCommand() : base()
        {
            _handManager = ServiceLocator.GetService<IHandManager>();
        }

        public override string GetDescription() => "Start Turn";

        public override bool CanExecute()
        {
            return _commandProcessor.CurrentState.Phase.CurrentPhase == GamePhase.TurnStart;
        }


        public override void Execute(CommandCompletionToken token)
        {
            _logger?.LogInfo($"[TurnStartCommand] Entered TurnStart phase");

            var cardsToDraw = _handManager.GetCardsToDraw();
            var drawCardCommands = new AddCardCommand[cardsToDraw];
            for (int i = 0; i < cardsToDraw; i++)
            {
                drawCardCommands[i] = _handManager.GetDrawCardCommand();
            }

            // Get the updated hand state after drawing
            var newHandState = _commandProcessor.CurrentState.Hand;

            // Transition to CardSelection phase (natural end state)
            var newPhaseState = _commandProcessor.CurrentState.Phase.WithPhase(GamePhase.CardSelection);
            var finalState = _commandProcessor.CurrentState
                .WithHand(newHandState)
                .WithPhase(newPhaseState);

            _logger?.LogInfo($"[TurnStartCommand] Turn started - cards drawn: {cardsToDraw}, final phase: {finalState.Phase.CurrentPhase}");

            token.Complete(CommandResult.Success(finalState, drawCardCommands));
        }
    }
}