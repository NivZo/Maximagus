using System;
using System.Linq;
using Scripts.State;
using Scripts.Config;
using Scripts.Commands;
using Maximagus.Scripts.Managers;

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
            return _commandProcessor.CurrentState.Phase.CurrentPhase == GamePhase.Menu ||
                   _commandProcessor.CurrentState.Phase.CurrentPhase == GamePhase.TurnEnd ||
                   _commandProcessor.CurrentState.Phase.CurrentPhase == GamePhase.GameStart;
        }


        public override CommandResult ExecuteWithResult()
        {
            // First transition to TurnStart phase (intermediate phase for processing)
            var turnStartPhaseState = _commandProcessor.CurrentState.Phase.WithPhase(GamePhase.TurnStart);
            var turnStartState = _commandProcessor.CurrentState.WithPhase(turnStartPhaseState);
            
            _logger?.LogInfo($"[TurnStartCommand] Entered TurnStart phase");

            // Draw cards to max hand size (pure computation)
            var cardsToDraw = _handManager.GetCardsToDraw();
            var currentHandState = turnStartState.Hand;
            
            for (int i = 0; i < cardsToDraw; i++)
            {
                _logger.LogInfo($"[TurnStartCommand] Drawing card {i + 1} of {cardsToDraw}");
                // Note: This still uses HandManager.DrawCard() which might have side effects
                // This should be refactored to be pure in a future iteration
                _handManager.DrawCard();
            }

            // Get the updated hand state after drawing
            var newHandState = _commandProcessor.CurrentState.Hand;

            // Transition to CardSelection phase (natural end state)
            var newPhaseState = _commandProcessor.CurrentState.Phase.WithPhase(GamePhase.CardSelection);
            var finalState = _commandProcessor.CurrentState
                .WithHand(newHandState)
                .WithPhase(newPhaseState);

            _logger?.LogInfo($"[TurnStartCommand] Turn started - cards drawn: {cardsToDraw}, final phase: {finalState.Phase.CurrentPhase}");

            // Events for turn start and card drawing
            var events = new object[]
            {
                new { Type = "TurnStarted", CardsDrawn = cardsToDraw },
                new { Type = "PhaseChanged", NewPhase = GamePhase.CardSelection, PreviousPhase = GamePhase.TurnStart },
                new { Type = "HandUpdated", NewHandState = newHandState }
            };

            return CommandResult.Success(finalState);
        }
    }
}