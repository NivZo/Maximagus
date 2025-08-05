using Scripts.State;
using Godot;
using System.Linq;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to remove a card from the player's hand in GameState
    /// Used when cards are discarded or removed
    /// </summary>
    public class RemoveCardCommand : IGameCommand
    {
        private readonly string _cardId;

        public RemoveCardCommand(string cardId)
        {
            _cardId = cardId;
        }

        public bool CanExecute(IGameStateData currentState)
        {
            if (currentState == null) return false;
            if (string.IsNullOrEmpty(_cardId)) return false;

            // Hand must not be locked
            if (currentState.Hand.IsLocked) return false;

            // Card must exist in hand
            foreach (var card in currentState.Hand.Cards)
            {
                if (card.CardId == _cardId) return true;
            }

            return false; // Card not found
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            GD.Print($"[RemoveCardCommand] Removing card {_cardId} from GameState");

            // Remove card from hand
            var newHandState = currentState.Hand.WithRemovedCard(_cardId);
            var newState = currentState.WithHand(newHandState);

            GD.Print($"[RemoveCardCommand] Card {_cardId} removed from GameState successfully. Hand now has {newHandState.Count} cards");
            return newState;
        }

        public string GetDescription()
        {
            return $"Remove card {_cardId} from hand";
        }
    }
}