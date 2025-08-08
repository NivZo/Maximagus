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

        public override CommandResult ExecuteWithResult()
        {
            var currentState = _commandProcessor.CurrentState;
            _logger?.LogInfo("[DiscardHandCommand] Executing discard command...");

            var selectedCardIds = currentState.Hand.SelectedCards.Select(c => c.CardId);
            var newPlayerState = currentState.Player.WithDiscardUsed();
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.CardSelection);
            var newHandState = currentState.Hand.WithRemovedCards(selectedCardIds);
            var newState = currentState
                .WithPlayer(newPlayerState)
                .WithPhase(newPhaseState)
                .WithHand(newHandState);

            _logger?.LogInfo($"[DiscardHandCommand] State updated: discards remaining: {newPlayerState.RemainingDiscards}");

            // Draw cards to refill hand (side effect handled by HandManager)
            _logger?.LogInfo("[DiscardHandCommand] Drawing cards to refill hand...");
            var cardsToDraw = _handManager.GetCardsToDraw();
            var drawCardCommands = new AddCardCommand[cardsToDraw];
            for (int i = 0; i < cardsToDraw; i++)
            {
                _logger.LogInfo($"[DiscardHandCommand] Drawing card {i + 1} of {cardsToDraw}");
                drawCardCommands[i] = _handManager.GetDrawCardCommand();
            }

            return CommandResult.Success(newState, drawCardCommands);
        }

        public override string GetDescription()
        {
            return "Discard selected cards";
        }
    }
}