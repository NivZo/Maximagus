using System;

public class DragManager : IDragManager
{
    private Card _currentlyDraggingCard;
    private readonly ILogger _logger;
    
    public bool IsDraggingActive => _currentlyDraggingCard != null;
    
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
            _logger.LogWarning($"Cannot start drag for {card.Name}, already dragging {_currentlyDraggingCard.Name}");
            return false;
        }
        
        _currentlyDraggingCard = card;
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
        
        if (_currentlyDraggingCard != card)
        {
            _logger.LogWarning($"Card mismatch when ending drag. Expected: {_currentlyDraggingCard?.Name}, Got: {card.Name}");
            return;
        }
        
        _currentlyDraggingCard = null;
        _logger.LogInfo($"Ended dragging card: {card.Name}");
    }
}