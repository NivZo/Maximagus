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
        private ILogger _logger;
        private IEventBus _eventBus;
        private IGameStateManager _turnStateMachine;

        public HandManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _eventBus = ServiceLocator.GetService<IEventBus>();
            _turnStateMachine = ServiceLocator.GetService<IGameStateManager>();
            ResetForNewEncounter();

            _eventBus.Subscribe<PlayCardsRequestedEvent>(HandlePlayCardsRequested);
            _eventBus.Subscribe<DiscardCardsRequestedEvent>(HandleDiscardCardsRequested);
        }

        private void HandlePlayCardsRequested(PlayCardsRequestedEvent e)
        {
            SubmitHand([.. e.SelectedCards.Select(c => c.Resource)], [.. e.CurrentHandCards.Select(c => c.Resource)], HandActionType.Play);
        }

        private void HandleDiscardCardsRequested(DiscardCardsRequestedEvent e)
        {
            SubmitHand([.. e.SelectedCards.Select(c => c.Resource)], [.. e.CurrentHandCards.Select(c => c.Resource)], HandActionType.Discard);
        }

        public void ResetForNewEncounter()
        {
            _remainingHands = MaxHandsPerEncounter;
            _remainingDiscards = MaxDiscardsPerEncounter;
            GD.Print($"Hand Manager reset: {_remainingHands} hands, {_remainingDiscards} discards available");
        }

        public bool CanSubmitHand(HandActionType actionType)
        {
            if (_turnStateMachine.GetCurrentState() is not SubmitPhaseState) return false;
            return actionType switch
            {
                HandActionType.Play => _remainingHands > 0,
                HandActionType.Discard => _remainingDiscards > 0,
                _ => false
            };
        }

        private void SubmitHand(SpellCardResource[] selectedCards, SpellCardResource[] currentHandCards, HandActionType actionType)
        {
            if (!CanSubmitHand(actionType))
            {
                _logger.LogWarning($"Cannot submit hand: no {actionType} actions remaining");
                return;
            }

            if (selectedCards.Length == 0 || selectedCards.Length > MaxCardsPerSubmission)
            {
                _logger.LogWarning($"Invalid card count: {selectedCards.Length} (max: {MaxCardsPerSubmission})");
                return;
            }

            foreach (var card in selectedCards)
            {
                if (!currentHandCards.Contains(card))
                {
                    _logger.LogWarning($"Card {card.CardName} not in current hand");
                    return;
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

            _turnStateMachine.TriggerEvent(actionType == HandActionType.Play ? GameStateEvent.HandSubmitted : GameStateEvent.HandDiscarded);

            if (_remainingHands == 0 && _remainingDiscards == 0)
            {
                _eventBus.Publish(new HandLimitReachedEvent
                {
                    RemainingHands = _remainingHands,
                    RemainingDiscards = _remainingDiscards
                });
            }
        }
    }
}