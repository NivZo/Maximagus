using System;
using System.Linq;
using Scripts.State;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to discard the currently selected cards without playing them
    /// </summary>
    public class DiscardHandCommand : GameCommand
    {
        private readonly IHandManager _handManager;

        public DiscardHandCommand() : base()
        {
            _handManager = ServiceLocator.GetService<IHandManager>();
        }

        public override bool CanExecute()
        {
            if (_commandProcessor.CurrentState == null) return false;

            // Can discard during card selection phase
            if (!_commandProcessor.CurrentState.Phase.AllowsCardSelection)
                return false;

            // Must have at least one card selected
            if (_commandProcessor.CurrentState.Hand.SelectedCount == 0) return false;

            // Hand must not be locked
            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            return true;
        }

        public override IGameStateData Execute()
        {
            _logger?.LogInfo("[DiscardHandCommand] Execute() called!");

            var newPlayerState = _commandProcessor.CurrentState.Player.WithDiscardUsed();
            var newPhaseState = _commandProcessor.CurrentState.Phase.WithPhase(GamePhase.CardSelection);
            var newHandState = _commandProcessor.CurrentState.Hand.WithRemovedCards(_commandProcessor.CurrentState.Hand.SelectedCards.Select(c => c.CardId));
            var newState = _commandProcessor.CurrentState
                .WithPlayer(newPlayerState)
                .WithPhase(newPhaseState)
                .WithHand(newHandState);
            _commandProcessor.SetState(newState);

            _logger?.LogInfo($"[DiscardHandCommand] State updated: discards remaining: {newState.Player.RemainingDiscards}");

            _logger?.LogInfo("[DiscardHandCommand] Queuing card draw to max hand size...");
            var cardsToDraw = _handManager.GetCardsToDraw();
            for (int i = 0; i < cardsToDraw; i++)
            {
                _logger.LogInfo($"[DiscardHandCommand] Drawing card {i + 1} of {cardsToDraw}");
                _handManager.DrawCard();
            }

            return _commandProcessor.CurrentState;
        }

        public override string GetDescription()
        {
            return "Discard selected cards";
        }
    }
}