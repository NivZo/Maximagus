using Maximagus.Scripts.Managers;
using Maximagus.Scripts.Spells.Abstractions;

/// <summary>
/// Represents a deck of cards that can be drawn from
/// Now uses ResourceManager for card resources to maintain state-driven design
/// </summary>
public class Deck
{
    private IResourceManager _resourceManager;
    private ILogger _logger;

    public Deck()
    {
        _logger = ServiceLocator.GetService<ILogger>();
        _resourceManager = ServiceLocator.GetService<IResourceManager>();
    }

    /// <summary>
    /// Gets the next card resource from the deck
    /// </summary>
    /// <returns>The SpellCardResource for the next card</returns>
    public SpellCardResource GetNext()
    {
        return _resourceManager.GetRandomSpellCardResource();
    }
}