using System;
using Scripts.State;
using System.Linq;
using Godot;
using Maximagus.Scripts.Managers;

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
                    return card.IsSelected; // Can only deselect if currently selected
                }
            }

            return false; // Card not found
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            Console.WriteLine($"[DeselectCardCommand] Execute() called for card {_cardId}!");
            
            // Get HandManager to access the Hand properly
            var handManager = ServiceLocator.GetService<IHandManager>();
            if (handManager?.Hand == null)
            {
                Console.WriteLine("[DeselectCardCommand] ERROR: HandManager.Hand is null!");
                return currentState;
            }

            // Find the real card by ID
            var realCard = handManager.Hand.Cards.FirstOrDefault(c => c.GetInstanceId().ToString() == _cardId);
            if (realCard == null)
            {
                Console.WriteLine($"[DeselectCardCommand] ERROR: Card {_cardId} not found in hand!");
                return currentState;
            }

            // NEW SYSTEM: Directly call the CardLogic SetSelected method
            if (realCard.IsSelected && realCard.Logic != null)
            {
                realCard.Logic.SetSelected(false);
                Console.WriteLine($"[DeselectCardCommand] Card {_cardId} deselected successfully via command system");
            }
            else
            {
                Console.WriteLine($"[DeselectCardCommand] Card {_cardId} was already deselected or Logic is null");
            }

            // Update GameState to keep it in sync
            var newHandState = currentState.Hand.WithCardSelection(_cardId, false);
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