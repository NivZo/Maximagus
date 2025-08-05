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
        public Hand Hand { get; private set; }

        public HandManager()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _eventBus = ServiceLocator.GetService<IEventBus>();
            ResetForNewEncounter();

            _eventBus.Subscribe<PlayCardsRequestedEvent>(HandlePlayCardsRequested);
            _eventBus.Subscribe<DiscardCardsRequestedEvent>(HandleDiscardCardsRequested);
        }

        public void SetupHandNode(Hand hand)
        {
            Hand = hand;
        }

        private void HandlePlayCardsRequested(PlayCardsRequestedEvent e)
        {
            // Let the new command system handle this - just publish the spell request
            _eventBus.Publish(new CastSpellRequestedEvent());
        }

        private void HandleDiscardCardsRequested(DiscardCardsRequestedEvent e)
        {
            // PURE COMMAND SYSTEM: Use instance methods on Hand, not static calls
            if (Hand != null && Hand.SelectedCards.Length > 0)
            {
                var selectedCards = Hand.SelectedCards.ToArray();
                
                // Call instance methods on the Hand object
                Hand.Discard(selectedCards);
                Hand.DrawAndAppend(selectedCards.Length);
                
                RemainingDiscards--;
                _logger?.LogInfo($"[HandManager] Discarded {selectedCards.Length} cards, {RemainingDiscards} discards remaining");
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
            return actionType switch
            {
                HandActionType.Play => RemainingHands > 0,
                HandActionType.Discard => RemainingDiscards > 0,
                _ => false
            };
        }
    }
}