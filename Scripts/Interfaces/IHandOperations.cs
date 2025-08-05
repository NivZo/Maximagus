using System.Collections.Generic;
using Scripts.State;
using Maximagus.Scripts.Enums;

namespace Scripts.Interfaces
{
    /// <summary>
    /// Interface for state-driven hand operations.
    /// Separates state queries from visual node manipulations to prepare for event-driven architecture.
    /// </summary>
    public interface IHandOperations
    {
        /// <summary>
        /// STATE-DRIVEN: Checks if a card can be added to the hand based on current game state
        /// </summary>
        bool CanAddCard(IGameStateData currentState);

        /// <summary>
        /// STATE-DRIVEN: Checks if a specific card can be removed from the hand
        /// </summary>
        bool CanRemoveCard(IGameStateData currentState, string cardId);

        /// <summary>
        /// STATE-DRIVEN: Checks if the current hand can be played
        /// </summary>
        bool CanPlayHand(IGameStateData currentState);

        /// <summary>
        /// STATE-DRIVEN: Checks if the current hand can be discarded
        /// </summary>
        bool CanDiscardHand(IGameStateData currentState);

        /// <summary>
        /// STATE-DRIVEN: Checks if a hand action (play/discard) is possible
        /// </summary>
        bool CanPerformHandAction(IGameStateData currentState, HandActionType actionType);

        /// <summary>
        /// VISUAL NODE OPERATION: Draws cards and adds them to the visual hand
        /// This affects the visual representation and will trigger state updates via commands
        /// </summary>
        void DrawCards(int count);

        /// <summary>
        /// VISUAL NODE OPERATION: Discards specific cards from the visual hand
        /// This affects the visual representation and will trigger state updates via commands
        /// </summary>
        void DiscardCards(IEnumerable<string> cardIds);

        /// <summary>
        /// VISUAL NODE OPERATION: Discards all selected cards from the visual hand
        /// This affects the visual representation and will trigger state updates via commands
        /// </summary>
        void DiscardSelectedCards();

        /// <summary>
        /// STATE-DRIVEN: Gets the count of cards that can be drawn to reach max hand size
        /// </summary>
        int GetCardsToDraw(IGameStateData currentState);

        /// <summary>
        /// STATE-DRIVEN: Gets the current hand status summary
        /// </summary>
        HandStatusSummary GetHandStatus(IGameStateData currentState);
    }

    /// <summary>
    /// Summary of hand status for UI and game logic
    /// </summary>
    public struct HandStatusSummary
    {
        public int CurrentCards { get; }
        public int MaxCards { get; }
        public int SelectedCards { get; }
        public bool IsLocked { get; }
        public bool HasDraggingCard { get; }
        public int RemainingHands { get; }
        public int RemainingDiscards { get; }

        public HandStatusSummary(
            int currentCards,
            int maxCards,
            int selectedCards,
            bool isLocked,
            bool hasDraggingCard,
            int remainingHands,
            int remainingDiscards)
        {
            CurrentCards = currentCards;
            MaxCards = maxCards;
            SelectedCards = selectedCards;
            IsLocked = isLocked;
            HasDraggingCard = hasDraggingCard;
            RemainingHands = remainingHands;
            RemainingDiscards = remainingDiscards;
        }

        public bool IsHandFull => CurrentCards >= MaxCards;
        public bool HasSelectedCards => SelectedCards > 0;
        public bool CanPlay => HasSelectedCards && RemainingHands > 0 && !IsLocked;
        public bool CanDiscard => HasSelectedCards && RemainingDiscards > 0 && !IsLocked;
    }
}