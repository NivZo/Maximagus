using System;
using System.Linq;
using Scripts.Commands;

public class HoverManager : IHoverManager
{
    private readonly ILogger _logger;
    private IGameCommandProcessor _commandProcessor;
    public Card CurrentlyHoveringCard { get; private set; }
    
    public bool IsHoveringActive => CurrentlyHoveringCard != null;
    
    public HoverManager()
    {
        _logger = ServiceLocator.GetService<ILogger>();
        _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
    }
    
    public bool StartHover(Card card)
    {
        if (card == null)
        {
            _logger.LogWarning("Attempted to start hover with null card");
            return false;
        }

        if (IsHoveringActive && _commandProcessor.CurrentState.Hand.Cards.Any(c => c.CardId == CurrentlyHoveringCard.CardId))
        {
            _logger.LogWarning($"Cannot start hover for {card.Name}, already hovering {CurrentlyHoveringCard.Name}");
            return false;
        }
        
        CurrentlyHoveringCard = card;
        _logger.LogDebug($"Started hovering card: {card.Name}");
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
        _logger.LogDebug($"Ended hovering card: {card.Name}");
    }
}