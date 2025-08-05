using Scripts.State;
using Godot;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to add a new card to the player's hand in GameState
    /// Used when cards are drawn/created after initial game setup
    /// </summary>
    public class AddCardCommand : IGameCommand
    {
        private readonly string _cardId;
        private readonly int _position;

        public AddCardCommand(string cardId, int position = -1)
        {
            _cardId = cardId;
            _position = position; // -1 means append to end
        }

        public bool CanExecute(IGameStateData currentState)
        {
            if (currentState == null) return false;
            if (string.IsNullOrEmpty(_cardId)) return false;

            // Hand must not be locked
            if (currentState.Hand.IsLocked) return false;

            // Check hand size limit
            if (currentState.Hand.Count >= currentState.Hand.MaxHandSize) return false;

            // Card must not already exist in hand
            foreach (var card in currentState.Hand.Cards)
            {
                if (card.CardId == _cardId) return false;
            }

            return true;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            GD.Print($"[AddCardCommand] Adding card {_cardId} to GameState at position {_position}");

            // Create new CardState for the added card
            var newCardState = new CardState(
                cardId: _cardId,
                isSelected: false,
                isDragging: false,
                position: _position >= 0 ? _position : currentState.Hand.Count
            );

            // Add card to hand
            var newHandState = currentState.Hand.WithAddedCard(newCardState);
            var newState = currentState.WithHand(newHandState);

            GD.Print($"[AddCardCommand] Card {_cardId} added to GameState successfully. Hand now has {newHandState.Count} cards");
            return newState;
        }

        public string GetDescription()
        {
            return $"Add card {_cardId} to hand at position {_position}";
        }
    }
}