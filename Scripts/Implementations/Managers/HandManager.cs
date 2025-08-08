using Godot;
using System;
using System.Collections.Immutable;
using System.Linq;
using Maximagus.Scripts.Enums;
using Scripts.State;
using Scripts.Commands;
using Maximagus.Scripts.Spells.Abstractions;
using Scripts.Commands.Hand;

namespace Maximagus.Scripts.Managers
{
    public class HandManager : IHandManager
    {        
        private ILogger _logger;
        private IGameCommandProcessor _commandProcessor;
        
        public ImmutableArray<SpellCardResource> Cards => _commandProcessor.CurrentState.Hand.Cards.Select(card => card.Resource).ToImmutableArray();
        public ImmutableArray<SpellCardResource> SelectedCards => _commandProcessor.CurrentState.Hand.Cards.Where(card => card.IsSelected).Select(card => card.Resource).ToImmutableArray();
        public SpellCardResource DraggingCard => _commandProcessor.CurrentState.Hand.DraggingCard.Resource;

        private IGameStateData _currentState => _commandProcessor.CurrentState;

        public HandManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        }

        public void ResetForNewEncounter()
        {
            GD.Print("Hand Manager reset - now uses PlayerState for remaining actions");
        }

        public bool CanSubmitHand(HandActionType actionType)
        {
            return _currentState.Player.CanPerformHandAction(actionType);
        }

        public bool CanAddCard()
        {
            return !_currentState.Hand.IsLocked &&
                   _currentState.Hand.Count < _currentState.Hand.MaxHandSize;
        }

        public bool CanRemoveCard(string cardId)
        {
            return !_currentState.Hand.IsLocked &&
                   _currentState.Hand.Cards.Any(c => c.CardId == cardId);
        }

        public bool CanPlayHand()
        {
            return _currentState.Hand.SelectedCount > 0 &&
                   !_currentState.Hand.IsLocked &&
                   _currentState.Player.HasHandsRemaining;
        }

        public bool CanDiscardHand()
        {
            return _currentState.Hand.SelectedCount > 0 &&
                   !_currentState.Hand.IsLocked &&
                   _currentState.Player.HasDiscardsRemaining;
        }

        public bool CanPerformHandAction(HandActionType actionType)
        {
            return actionType switch
            {
                HandActionType.Play => CanPlayHand(),
                HandActionType.Discard => CanDiscardHand(),
                _ => false
            };
        }

        public int GetCardsToDraw()
        {
            return Math.Max(0, _currentState.Hand.MaxHandSize - _currentState.Hand.Count);
        }

        public HandStatusSummary GetHandStatus()
        {
            return new HandStatusSummary(
                currentCards: _currentState.Hand.Count,
                maxCards: _currentState.Hand.MaxHandSize,
                selectedCards: _currentState.Hand.SelectedCount,
                isLocked: _currentState.Hand.IsLocked,
                hasDraggingCard: _currentState.Hand.HasDraggingCard,
                remainingHands: _currentState.Player.RemainingHands,
                remainingDiscards: _currentState.Player.RemainingDiscards
            );
        }

        public AddCardCommand GetDrawCardCommand()
        {
            var deck = new Deck();
            var resource = deck.GetNext();
            var command = new AddCardCommand(resource);
            return command;
        }

        public void DiscardCard(string cardId)
        {
            var newHandState = _currentState.Hand.WithRemovedCard(cardId);
            _commandProcessor.SetState(_currentState.WithHand(newHandState));
        }

        public void DiscardSelectedCards()
        {
            var newState = _currentState;
            foreach (var card in _currentState.Hand.SelectedCards)
            {
                _logger?.LogInfo($"[HandManager] Discarding card: {card.CardId}");
                newState = newState.WithHand(newState.Hand.WithRemovedCard(card.CardId));
            }

            _commandProcessor.SetState(newState);
        }
    }
}