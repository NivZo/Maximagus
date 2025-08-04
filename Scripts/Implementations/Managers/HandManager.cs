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
        public int MaxHandsPerEncounter { get; set; } = 5;
        public int MaxDiscardsPerEncounter { get; set; } = 5;
        public int MaxCardsInHand { get; set; } = 10;
        public int MaxCardsPerSubmission { get; set; } = 4;
        
        public int RemainingHands { get; private set; }
        public int RemainingDiscards { get; private set; }

        private ILogger _logger;
        private IEventBus _eventBus;
        private IGameStateManager _turnStateMachine;
        public Hand Hand { get; private set; }

        public HandManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _eventBus = ServiceLocator.GetService<IEventBus>();
            _turnStateMachine = ServiceLocator.GetService<IGameStateManager>();
            ResetForNewEncounter();

            _eventBus.Subscribe<PlayCardsRequestedEvent>(HandlePlayCardsRequested);
            _eventBus.Subscribe<DiscardCardsRequestedEvent>(HandleDiscardCardsRequested);
            _eventBus.Subscribe<GameStateChangedEvent>(HandleGameStateChanged);
        }

        public void SetupHandNode(Hand hand)
        {
            Hand = hand;
        }

        private void HandlePlayCardsRequested(PlayCardsRequestedEvent e)
        {
            SubmitHand(Hand.SelectedCards.Select(c => c.Resource).ToArray(), HandActionType.Play);
        }

        private void HandleDiscardCardsRequested(DiscardCardsRequestedEvent e)
        {
            SubmitHand(Hand.SelectedCards.Select(c => c.Resource).ToArray(), HandActionType.Discard);
        }

        private void HandleGameStateChanged(GameStateChangedEvent e)
        {
            if (e.PreviousState is SpellCastState ||
                (e.PreviousState is SubmitPhaseState && e.NewState is SubmitPhaseState))
            {
                var count = Hand.SelectedCards.Count();
                if (count > 0)
                {
                    Hand.Discard(Hand.SelectedCards);
                    Hand.DrawAndAppend(count);
                }
            }

        }

        public void ResetForNewEncounter()
        {
            RemainingHands = MaxHandsPerEncounter;
            RemainingDiscards = MaxDiscardsPerEncounter;
            GD.Print($"Hand Manager reset: {RemainingHands} hands, {RemainingDiscards} discards available");
        }

        public bool CanSubmitHand(HandActionType actionType)
        {
            if (_turnStateMachine.GetCurrentState() is not SubmitPhaseState) return false;
            return actionType switch
            {
                HandActionType.Play => RemainingHands > 0,
                HandActionType.Discard => RemainingDiscards > 0,
                _ => false
            };
        }

        private void SubmitHand(SpellCardResource[] selectedCards, HandActionType actionType)
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

            if (actionType == HandActionType.Play)
                RemainingHands--;
            else
                RemainingDiscards--;

            _turnStateMachine.TriggerEvent(actionType == HandActionType.Play ? GameStateEvent.HandSubmitted : GameStateEvent.HandDiscarded);
        }
    }
}