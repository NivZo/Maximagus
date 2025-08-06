using System.Collections.Generic;
using Maximagus.Scripts.Enums;
using Scripts.State;

public interface IHandManager
{
    Hand Hand { get; }

    void SetupHandNode(Hand hand);

    void ResetForNewEncounter();

    bool CanSubmitHand(HandActionType actionType);

    bool CanAddCard(IGameStateData currentState);

    bool CanRemoveCard(IGameStateData currentState, string cardId);

    bool CanPlayHand(IGameStateData currentState);

    bool CanDiscardHand(IGameStateData currentState);

    bool CanPerformHandAction(IGameStateData currentState, HandActionType actionType);

    /// <summary>
    /// Draws a card from the deck and returns its resource ID
    /// This follows state-driven architecture by only getting the card ID
    /// The caller should then update the state, which will trigger UI updates
    /// </summary>
    /// <returns>The ID of the drawn card resource</returns>
    string DrawCard();
    
    /// <summary>
    /// Legacy method - should be removed once state-driven approach is fully implemented
    /// </summary>
    void DrawCards(int count);

    void DiscardCards(IEnumerable<string> cardIds);

    void DiscardSelectedCards();

    int GetCardsToDraw(IGameStateData currentState);

    HandStatusSummary GetHandStatus(IGameStateData currentState);
}
    

    
    
    
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