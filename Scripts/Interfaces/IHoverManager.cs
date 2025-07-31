public interface IHoverManager
{
    Card CurrentlyHoveringCard { get; }
    bool IsHoveringActive { get; }
    bool StartHover(Card card);
    void EndHover(Card card);
}