using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Events;
using System.Linq;

namespace Maximagus.Scripts.Managers
{
    public partial class HandManager : IHandManager
    {
        [Export] public int MaxHandsPerEncounter { get; set; } = 5;
        [Export] public int MaxDiscardsPerEncounter { get; set; } = 5;
        [Export] public int MaxCardsInHand { get; set; } = 10;
        [Export] public int MaxCardsPerSubmission { get; set; } = 4;
        
        public int RemainingHands => _remainingHands;
        public int RemainingDiscards => _remainingDiscards;

        private int _remainingHands;
        private int _remainingDiscards;
        private IEventBus _eventBus;

        public HandManager()
        {
            _eventBus = ServiceLocator.GetService<IEventBus>();
            ResetForNewEncounter();
        }
        
        public void ResetForNewEncounter()
        {
            _remainingHands = MaxHandsPerEncounter;
            _remainingDiscards = MaxDiscardsPerEncounter;
            GD.Print($"Hand Manager reset: {_remainingHands} hands, {_remainingDiscards} discards available");
        }

        public bool CanSubmitHand(HandActionType actionType)
        {
            return actionType switch
            {
                HandActionType.Play => _remainingHands > 0,
                HandActionType.Discard => _remainingDiscards > 0,
                _ => false
            };
        }

        public bool SubmitHand(Array<SpellCardResource> selectedCards, HandActionType actionType)
        {
            if (!CanSubmitHand(actionType))
            {
                GD.Print($"Cannot submit hand: no {actionType} actions remaining");
                return false;
            }

            if (selectedCards.Count == 0 || selectedCards.Count > MaxCardsPerSubmission)
            {
                GD.Print($"Invalid card count: {selectedCards.Count} (max: {MaxCardsPerSubmission})");
                return false;
            }

            var currentHandCards = Hand.Instance.Cards.Select(c => c.Resource).ToList();
            foreach (var card in selectedCards)
            {
                if (!currentHandCards.Contains(card))
                {
                    GD.Print($"Card {card.CardName} not in current hand");
                    return false;
                }
            }

            if (actionType == HandActionType.Play)
                _remainingHands--;
            else
                _remainingDiscards--;

            _eventBus.Publish(new HandSubmittedEvent 
            { 
                Cards = selectedCards, 
                ActionType = actionType 
            });

            GD.Print($"{actionType} action completed. Remaining: {_remainingHands} hands, {_remainingDiscards} discards");

            if (_remainingHands == 0 && _remainingDiscards == 0)
            {
                _eventBus.Publish(new HandLimitReachedEvent 
                { 
                    RemainingHands = _remainingHands, 
                    RemainingDiscards = _remainingDiscards 
                });
            }

            return true;
        }
    }
}