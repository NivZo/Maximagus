public interface IDragManager
{
    Card CurrentlyDraggingCard { get; }
    bool IsDraggingActive { get; }
    bool StartDrag(Card card);
    void EndDrag(Card card);
}