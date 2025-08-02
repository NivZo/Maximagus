using System;

public class HoverManager : IHoverManager
{
    public Card CurrentlyHoveringCard { get; private set; }
    private readonly ILogger _logger;
    
    public bool IsHoveringActive => CurrentlyHoveringCard != null;
    
    public HoverManager()
    {
        _logger = ServiceLocator.GetService<ILogger>();
    }
    
    public bool StartHover(Card card)
    {
        if (card == null)
        {
            _logger.LogWarning("Attempted to start hover with null card");
            return false;
        }
        
        if (IsHoveringActive)
        {
            _logger.LogWarning($"Cannot start hover for {card.Name}, already hovering {CurrentlyHoveringCard.Name}");
            return false;
        }
        
        CurrentlyHoveringCard = card;
        _logger.LogInfo($"Started hovering card: {card.Name}");
        return true;
    }
    
    public void EndHover(Card card)
    {
        if (card == null)
        {
            _logger.LogWarning("Attempted to end hover with null card");
            return;
        }
        
        if (CurrentlyHoveringCard != card)
        {
            _logger.LogWarning($"Card mismatch when ending hover. Expected: {CurrentlyHoveringCard?.Name}, Got: {card.Name}");
            return;
        }
        
        CurrentlyHoveringCard = null;
        _logger.LogInfo($"Ended hovering card: {card.Name}");
    }
}