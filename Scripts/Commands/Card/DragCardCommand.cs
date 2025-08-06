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
            if (string.IsNullOrEmpty(_cardId)) return false;
            
            // Check if card exists in hand
            var cardExists = _commandProcessor.CurrentState.Hand.Cards.Any(card => card.CardId == _cardId);
            if (!cardExists) return false;

            var anyCardDragging = _commandProcessor.CurrentState.Hand.Cards.Any(card => card.IsDragging);
            if (anyCardDragging) return false;
            
            // Hand must not be locked
            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            return true;
        }

        public override IGameStateData Execute()
        {
            GD.Print($"[StartDragCommand] Starting drag for card {_cardId}");
            
            // Update the specific card to be dragging
            var newHandState = _commandProcessor.CurrentState.Hand.WithCardDragging(_cardId, true);
            var newState = _commandProcessor.CurrentState.WithHand(newHandState);

            GD.Print($"[StartDragCommand] Card {_cardId} is now dragging");
            return newState;
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
            var cardState = _commandProcessor.CurrentState.Hand.Cards.FirstOrDefault(card => card.CardId == _cardId);
            return cardState?.IsDragging == true;
        }

        public override IGameStateData Execute()
        {
            GD.Print($"[EndDragCommand] Ending drag for card {_cardId}");

            var newHandState = _commandProcessor.CurrentState.Hand.WithCardDragging(_cardId, false);
            var newState = _commandProcessor.CurrentState.WithHand(newHandState);
            
            GD.Print($"[EndDragCommand] Card {_cardId} drag ended");
            return newState;
        }

        public override string GetDescription() => $"End dragging card: {_cardId}";
    }
}