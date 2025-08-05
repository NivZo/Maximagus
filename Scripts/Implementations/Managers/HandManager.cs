using Godot;
using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Events;
using System.Linq;

namespace Maximagus.Scripts.Managers
{
    /// <summary>
    /// CLEANED UP: HandManager with legacy event system removed
    /// Now purely manages hand state without event subscriptions
    /// </summary>
    public class HandManager : IHandManager
    {
        public int MaxHandsPerEncounter { get; set; } = 5;
        public int MaxDiscardsPerEncounter { get; set; } = 5;
        public int MaxCardsInHand { get; set; } = 10;
        public int MaxCardsPerSubmission { get; set; } = 4;
        
        public int RemainingHands { get; private set; }
        public int RemainingDiscards { get; private set; }

        private ILogger _logger;
        private IEventBus _eventBus;
        public Hand Hand { get; private set; }

        public HandManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _eventBus = ServiceLocator.GetService<IEventBus>();
            ResetForNewEncounter();

            // REMOVED: Legacy event subscriptions - replaced by pure command system
            // _eventBus.Subscribe<PlayCardsRequestedEvent>(HandlePlayCardsRequested);
            // _eventBus.Subscribe<DiscardCardsRequestedEvent>(HandleDiscardCardsRequested);
            
            _logger?.LogInfo("HandManager initialized without legacy event subscriptions");
        }

        public void SetupHandNode(Hand hand)
        {
            Hand = hand;
        }

        // REMOVED: Legacy event handlers - replaced by pure command system
        // private void HandlePlayCardsRequested(PlayCardsRequestedEvent e)
        // private void HandleDiscardCardsRequested(DiscardCardsRequestedEvent e)

        public void ResetForNewEncounter()
        {
            RemainingHands = MaxHandsPerEncounter;
            RemainingDiscards = MaxDiscardsPerEncounter;
            GD.Print($"Hand Manager reset: {RemainingHands} hands, {RemainingDiscards} discards available");
        }

        public bool CanSubmitHand(HandActionType actionType)
        {
            return actionType switch
            {
                HandActionType.Play => RemainingHands > 0,
                HandActionType.Discard => RemainingDiscards > 0,
                _ => false
            };
        }
    }
}