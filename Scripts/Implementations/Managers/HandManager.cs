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
            _logger.LogInfo("HandlePlayCardsRequested called - processing play cards request");
            SubmitHand(Hand.SelectedCards.Select(c => c.Resource).ToArray(), HandActionType.Play);
        }

        private void HandleDiscardCardsRequested(DiscardCardsRequestedEvent e)
        {
            _logger.LogInfo("HandleDiscardCardsRequested called - processing discard cards request");
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
            var currentState = _turnStateMachine.GetCurrentState();
            
            // Debug logging to understand what's happening
            _logger.LogInfo($"CanSubmitHand check: ActionType={actionType}, CurrentState={currentState?.GetType().Name}, RemainingHands={RemainingHands}, RemainingDiscards={RemainingDiscards}");
            
            if (currentState is not SubmitPhaseState)
            {
                _logger.LogInfo($"Cannot submit hand: current state is {currentState?.GetType().Name}, need SubmitPhaseState");
                return false;
            }

            var canSubmit = actionType switch
            {
                HandActionType.Play => RemainingHands > 0,
                HandActionType.Discard => RemainingDiscards > 0,
                _ => false
            };
            
            _logger.LogInfo($"CanSubmitHand result: {canSubmit}");
            return canSubmit;
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