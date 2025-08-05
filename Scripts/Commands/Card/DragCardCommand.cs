using Scripts.State;
using Godot;
using System.Linq;

namespace Scripts.Commands.Card
{
    /// <summary>
    /// Command to start dragging a card in the command system
    /// </summary>
    public class StartDragCommand : IGameCommand
    {
        private readonly string _cardId;
        private readonly Vector2 _mouseOffset;

        public StartDragCommand(string cardId, Vector2 mouseOffset)
        {
            _cardId = cardId;
            _mouseOffset = mouseOffset;
        }

        public bool CanExecute(IGameStateData currentState)
        {
            if (string.IsNullOrEmpty(_cardId)) return false;
            
            // Check if card exists in hand
            var cardExists = currentState.Hand.Cards.Any(card => card.CardId == _cardId);
            if (!cardExists) return false;
            
            // Check if no other card is currently being dragged
            var anyCardDragging = currentState.Hand.Cards.Any(card => card.IsDragging);
            if (anyCardDragging) return false;
            
            // Hand must not be locked
            if (currentState.Hand.IsLocked) return false;
            
            return true;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            GD.Print($"[StartDragCommand] Starting drag for card {_cardId}");
            
            // Update the specific card to be dragging
            var newHandState = currentState.Hand.WithCardDragging(_cardId, true);
            var newState = currentState.WithHand(newHandState);
            
            GD.Print($"[StartDragCommand] Card {_cardId} is now dragging");
            return newState;
        }

        public string GetDescription() => $"Start dragging card: {_cardId}";
    }

    /// <summary>
    /// Command to end dragging a card in the command system
    /// </summary>
    public class EndDragCommand : IGameCommand
    {
        private readonly string _cardId;

        public EndDragCommand(string cardId)
        {
            _cardId = cardId;
        }

        public bool CanExecute(IGameStateData currentState)
        {
            if (string.IsNullOrEmpty(_cardId)) return false;
            
            // Check if the specific card is currently being dragged
            var cardState = currentState.Hand.Cards.FirstOrDefault(card => card.CardId == _cardId);
            return cardState?.IsDragging == true;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            GD.Print($"[EndDragCommand] Ending drag for card {_cardId}");
            
            // Update the specific card to not be dragging
            var newHandState = currentState.Hand.WithCardDragging(_cardId, false);
            var newState = currentState.WithHand(newHandState);
            
            GD.Print($"[EndDragCommand] Card {_cardId} drag ended");
            return newState;
        }

        public string GetDescription() => $"End dragging card: {_cardId}";
    }
}