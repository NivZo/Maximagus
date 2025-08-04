using System;
using Scripts.State;

namespace Scripts.Commands.Card
{
    /// <summary>
    /// Command to deselect a card in the player's hand
    /// </summary>
    public class DeselectCardCommand : IGameCommand
    {
        private readonly string _cardId;

        public DeselectCardCommand(string cardId)
        {
            _cardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
        }

        public bool CanExecute(IGameStateData currentState)
        {
            if (currentState == null) return false;

            // Can only deselect cards during phases that allow player action
            if (!currentState.Phase.CanPlayerAct) return false;

            // Hand must not be locked
            if (currentState.Hand.IsLocked) return false;

            // Card must exist in hand and be selected
            foreach (var card in currentState.Hand.Cards)
            {
                if (card.CardId == _cardId)
                {
                    return card.IsSelected;
                }
            }

            return false; // Card not found
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            if (!CanExecute(currentState))
                throw new InvalidOperationException($"Cannot execute DeselectCardCommand for card {_cardId}");

            // Update hand state to deselect the card
            var newHandState = currentState.Hand.WithCardSelection(_cardId, false);

            // Return new game state with updated hand
            return currentState.WithHand(newHandState);
        }

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            return new SelectCardCommand(_cardId);
        }

        public string GetDescription()
        {
            return $"Deselect card: {_cardId}";
        }
    }
}