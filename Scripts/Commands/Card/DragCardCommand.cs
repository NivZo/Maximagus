using Scripts.State;
using Godot;
using System.Linq;

namespace Scripts.Commands.Card
{
    /// <summary>
    /// Command to start dragging a card in the command system
    /// </summary>
    public class StartDragCommand : GameCommand
    {
        private readonly string _cardId;

        public StartDragCommand(string cardId) : base()
        {
            _cardId = cardId;
        }

        public override bool CanExecute()
        {
            if (!_commandProcessor.CurrentState.Phase.AllowsCardSelection) return false;
            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            var cardExistsInHand = _commandProcessor.CurrentState.Cards.Cards
                .Any(card => card.CardId == _cardId && card.ContainerType == ContainerType.Hand);
            if (!cardExistsInHand) return false;

            var anyCardDragging = _commandProcessor.CurrentState.Cards.HasDragging;
            if (anyCardDragging) return false;

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            _logger.LogInfo($"[StartDragCommand] Starting drag for card {_cardId}");
            
            // Clear hover state and set dragging
            var newCards = currentState.Cards
                .WithCardHovering(_cardId, false)
                .WithCardDragging(_cardId, true);
            var newState = currentState.WithCards(newCards);

            _logger.LogInfo($"[StartDragCommand] Card {_cardId} is now dragging");

            token.Complete(CommandResult.Success(newState));
        }

        public override string GetDescription() => $"Start dragging card: {_cardId}";
    }

    public class EndDragCommand : GameCommand
    {
        private readonly string _cardId;

        public EndDragCommand(string cardId) : base()
        {
            _cardId = cardId;
        }

        public override bool CanExecute()
        {
            if (string.IsNullOrEmpty(_cardId)) return false;
            
            // Check if the specific card is currently being dragged
            var cardState = _commandProcessor.CurrentState.Cards.Cards
                .FirstOrDefault(card => card.CardId == _cardId);
            return cardState?.IsDragging == true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            _logger.LogInfo($"[EndDragCommand] Ending drag for card {_cardId}");

            var newCards = currentState.Cards.WithCardDragging(_cardId, false);
            var newState = currentState.WithCards(newCards);
            
            _logger.LogInfo($"[EndDragCommand] Card {_cardId} drag ended");

            token.Complete(CommandResult.Success(newState));
        }

        public override string GetDescription() => $"End dragging card: {_cardId}";
    }
}