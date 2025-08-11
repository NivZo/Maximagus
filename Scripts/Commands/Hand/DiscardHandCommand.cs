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

            // Must have at least one card selected in Hand
            if (!_commandProcessor.CurrentState.Cards.SelectedInHand.Any()) return false;

            // Hand must not be locked
            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            _logger?.LogInfo("[DiscardHandCommand] Executing discard command...");

            var selectedCardIds = currentState.Cards.SelectedInHand.Select(c => c.CardId).ToArray();

            // Move selected hand cards to Discarded, then clear any hand selections
            var moved = currentState.Cards.WithMovedToContainer(selectedCardIds, ContainerType.DiscardedCards);
            var newCards = moved.WithClearedSelection();

            var newPlayerState = currentState.Player.WithDiscardUsed();
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.CardSelection);
            var newState = currentState
                .WithCards(newCards)
                .WithPlayer(newPlayerState)
                .WithPhase(newPhaseState);

            _logger?.LogInfo($"[DiscardHandCommand] State updated: discards remaining: {newPlayerState.RemainingDiscards}");

            // Draw cards to refill hand (side effect handled by HandManager)
            _logger?.LogInfo("[DiscardHandCommand] Drawing cards to refill hand...");
            var cardsToDraw = selectedCardIds.Length;
            var drawCardCommands = new AddCardCommand[cardsToDraw];
            for (int i = 0; i < cardsToDraw; i++)
            {
                _logger.LogInfo($"[DiscardHandCommand] Drawing card {i + 1} of {cardsToDraw}");
                drawCardCommands[i] = _handManager.GetDrawCardCommand();
            }

            var result = CommandResult.Success(newState, drawCardCommands);
            token.Complete(result);
        }

        public override string GetDescription()
        {
            return "Discard selected cards";
        }
    }
}