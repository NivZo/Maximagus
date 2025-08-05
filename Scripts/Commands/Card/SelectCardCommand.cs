using System;
using Scripts.State;
using System.Linq;
using Godot;

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
            GD.Print($"[SelectCardCommand] Execute() called for card {_cardId}!");
            
            // COMMAND SYSTEM: Update only GameState - CardLogic will sync with this
            var newHandState = currentState.Hand.WithCardSelection(_cardId, true);
            var newState = currentState.WithHand(newHandState);
            
            GD.Print($"[SelectCardCommand] Card {_cardId} selected in GameState");
            return newState;
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