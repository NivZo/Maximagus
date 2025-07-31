using System;

public class DragManager : IDragManager
{
    private readonly ILogger _logger;
    
    public Card CurrentlyDraggingCard { get; private set; }
    public bool IsDraggingActive => CurrentlyDraggingCard != null;
    
    public DragManager(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public bool StartDrag(Card card)
    {
        if (card == null)
        {
            _logger.LogWarning("Attempted to start drag with null card");
            return false;
        }
        
        if (IsDraggingActive)
        {
            _logger.LogWarning($"Cannot start drag for {card.Name}, already dragging {CurrentlyDraggingCard.Name}");
            return false;
        }
        
        CurrentlyDraggingCard = card;
        _logger.LogInfo($"Started dragging card: {card.Name}");
        return true;
    }
    
    public void EndDrag(Card card)
    {
        if (card == null)
        {
            _logger.LogWarning("Attempted to end drag with null card");
            return;
        }
        
        if (CurrentlyDraggingCard != card)
        {
            _logger.LogWarning($"Card mismatch when ending drag. Expected: {CurrentlyDraggingCard?.Name}, Got: {card.Name}");
            return;
        }
        
        CurrentlyDraggingCard = null;
        _logger.LogInfo($"Ended dragging card: {card.Name}");
    }
}