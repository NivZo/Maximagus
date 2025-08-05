using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Maximagus.Scripts.Enums;
using Scripts.State;

namespace Maximagus.Scripts.Managers
{
    public class HandManager : IHandManager
    {
        public int MaxCardsPerSubmission { get; set; } = 4;
        
        private ILogger _logger;
        private Hand _hand;
        
        public ImmutableArray<Card> Cards => _hand?.Cards ?? ImmutableArray<Card>.Empty;
        public ImmutableArray<Card> SelectedCards => _hand?.SelectedCards ?? ImmutableArray<Card>.Empty;
        public Card DraggingCard => _hand?.DraggingCard;

        public HandManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
        }

        public Hand Hand => _hand;

        public void SetupHandNode(Hand hand)
        {
            _hand = hand;
        }

        public void ResetForNewEncounter()
        {
            GD.Print("Hand Manager reset - now uses PlayerState for remaining actions");
        }

        public bool CanSubmitHand(HandActionType actionType)
        {
            var gameStateManager = ServiceLocator.GetService<IGameStateManager>();
            var currentState = gameStateManager?.CurrentState;
            return currentState?.Player.CanPerformHandAction(actionType) ?? false;
        }
        public bool CanAddCard(IGameStateData currentState)
        {
            return !currentState.Hand.IsLocked &&
                   currentState.Hand.Count < currentState.Hand.MaxHandSize;
        }

        public bool CanRemoveCard(IGameStateData currentState, string cardId)
        {
            return !currentState.Hand.IsLocked &&
                   currentState.Hand.Cards.Any(c => c.CardId == cardId);
        }

        public bool CanPlayHand(IGameStateData currentState)
        {
            return currentState.Hand.SelectedCount > 0 &&
                   !currentState.Hand.IsLocked &&
                   currentState.Player.HasHandsRemaining;
        }

        public bool CanDiscardHand(IGameStateData currentState)
        {
            return currentState.Hand.SelectedCount > 0 &&
                   !currentState.Hand.IsLocked &&
                   currentState.Player.HasDiscardsRemaining;
        }

        public bool CanPerformHandAction(IGameStateData currentState, HandActionType actionType)
        {
            return actionType switch
            {
                HandActionType.Play => CanPlayHand(currentState),
                HandActionType.Discard => CanDiscardHand(currentState),
                _ => false
            };
        }

        public int GetCardsToDraw(IGameStateData currentState)
        {
            return Math.Max(0, currentState.Hand.MaxHandSize - currentState.Hand.Count);
        }

        public HandStatusSummary GetHandStatus(IGameStateData currentState)
        {
            return new HandStatusSummary(
                currentCards: currentState.Hand.Count,
                maxCards: currentState.Hand.MaxHandSize,
                selectedCards: currentState.Hand.SelectedCount,
                isLocked: currentState.Hand.IsLocked,
                hasDraggingCard: currentState.Hand.HasDraggingCard,
                remainingHands: currentState.Player.RemainingHands,
                remainingDiscards: currentState.Player.RemainingDiscards
            );
        }

        public void DrawCards(int count)
        {
            _hand?.DrawAndAppend(count);
        }

        public void DiscardCards(IEnumerable<string> cardIds)
        {
            if (_hand == null) return;
            
            var cardsToDiscard = _hand.Cards
                .Where(card => cardIds.Contains(card.GetInstanceId().ToString()))
                .ToArray();
                
            if (cardsToDiscard.Length > 0)
            {
                _hand.Discard(cardsToDiscard);
            }
        }

        public void DiscardSelectedCards()
        {
            var selectedCards = _hand?.SelectedCards;
            if (selectedCards.HasValue && selectedCards.Value.Length > 0)
            {
                _hand.Discard(selectedCards.Value);
            }
        }
    }
}