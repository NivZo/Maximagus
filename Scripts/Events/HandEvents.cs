public class HandCardSlotsChangedEvent
{
    public Hand Hand { get; }
    public HandCardSlotsChangedEvent(Hand hand) => Hand = hand;
}