using System;
using Scripts.State;
using System.Linq;
using Godot;
using Maximagus.Scripts.Managers;

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
            Console.WriteLine($"[SelectCardCommand] Execute() called for card {_cardId}!");
            
            // Get HandManager to access the Hand properly
            var handManager = ServiceLocator.GetService<IHandManager>();
            if (handManager?.Hand == null)
            {
                Console.WriteLine("[SelectCardCommand] ERROR: HandManager.Hand is null!");
                return currentState;
            }

            // Find the real card by ID
            var realCard = handManager.Hand.Cards.FirstOrDefault(c => c.GetInstanceId().ToString() == _cardId);
            if (realCard == null)
            {
                Console.WriteLine($"[SelectCardCommand] ERROR: Card {_cardId} not found in hand!");
                return currentState;
            }

            // Select the real card by simulating a click
            if (!realCard.IsSelected)
            {
                var mouseEvent = new InputEventMouseButton();
                mouseEvent.ButtonIndex = MouseButton.Left;
                mouseEvent.Pressed = false; // Release event triggers the click
                realCard.Logic.OnGuiInput(mouseEvent);
                Console.WriteLine($"[SelectCardCommand] Card {_cardId} selected successfully");
            }
            else
            {
                Console.WriteLine($"[SelectCardCommand] Card {_cardId} was already selected");
            }

            // Update GameState to keep it in sync
            var newHandState = currentState.Hand.WithCardSelection(_cardId, true);
            return currentState.WithHand(newHandState);
        }

        public string GetDescription()
        {
            return $"Select card: {_cardId}";
        }
    }
}