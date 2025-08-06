using System;
using System.Linq;
using Scripts.State;
using Scripts.Config;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Game
{
    /// <summary>
    /// Command to handle turn start - draws cards to max hand size and transitions to CardSelection
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
            return _commandProcessor.CurrentState.Phase.CurrentPhase == GamePhase.Menu || _commandProcessor.CurrentState.Phase.CurrentPhase == GamePhase.TurnEnd;
        }

        public override IGameStateData Execute()
        {
            _commandProcessor.SetState(_commandProcessor.CurrentState.WithPhase(_commandProcessor.CurrentState.Phase.WithPhase(GamePhase.TurnStart)));
            _logger?.LogInfo($"[TurnStartCommand] Current phase: {_commandProcessor.CurrentState.Phase.CurrentPhase}");

            _logger?.LogInfo("[TurnStartCommand] Queuing card draw to max hand size...");
            var cardsToDraw = _handManager.GetCardsToDraw();
            for (int i = 0; i < cardsToDraw; i++)
            {
                _logger.LogInfo($"[TurnStartCommand] Drawing card {i + 1} of {cardsToDraw}");
                _handManager.DrawCard();
            }

            var newPhaseState = _commandProcessor.CurrentState.Phase.WithPhase(GamePhase.CardSelection);
            var newState = _commandProcessor.CurrentState.WithPhase(newPhaseState);
            _logger?.LogInfo($"[TurnStartCommand] Turn started - new phase: {newState.Phase.CurrentPhase}");

            return newState;
        }
    }
}