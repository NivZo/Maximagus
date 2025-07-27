public interface IDragManager
{
    bool IsDraggingActive { get; }
    bool StartDrag(Card card);
    void EndDrag(Card card);
}