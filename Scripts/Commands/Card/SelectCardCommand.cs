using System;
using Scripts.State;
using System.Linq;
using Godot;
using GlobalHand = Hand; // Alias to avoid namespace conflict

namespace Scripts.Commands.Card
{
    /// <summary>
    /// Command to select a card in the player's hand
    /// </summary>
    public class SelectCardCommand : IGameCommand
    {
        private readonly string _cardId;

        public SelectCardCommand(string cardId)
        {
            _cardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
        }

        public bool CanExecute(IGameStateData currentState)
        {
            if (currentState == null) return false;

            // Can only select cards during phases that allow player action
            if (!currentState.Phase.CanPlayerAct) return false;

            // Hand must not be locked
            if (currentState.Hand.IsLocked) return false;

            // Card must exist in hand
            var cardExists = false;
            foreach (var card in currentState.Hand.Cards)
            {
                if (card.CardId == _cardId)
                {
                    cardExists = true;
                    // Card must not already be selected
                    if (card.IsSelected) return false;
                    break;
                }
            }

            return cardExists;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            Console.WriteLine($"[SelectCardCommand] Execute() called for card {_cardId} - updating GameState!");
            
            if (!CanExecute(currentState))
                throw new InvalidOperationException($"Cannot execute SelectCardCommand for card {_cardId}");

            // Update hand state to select the card
            var newHandState = currentState.Hand.WithCardSelection(_cardId, true);
            
            Console.WriteLine($"[SelectCardCommand] Card {_cardId} selected in GameState");

            // Return new game state with updated hand
            return currentState.WithHand(newHandState);
        }

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            return new DeselectCardCommand(_cardId);
        }

        public string GetDescription()
        {
            return $"Select card: {_cardId}";
        }
    }
}