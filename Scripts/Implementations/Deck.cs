using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Maximagus.Scripts.Managers;
using Maximagus.Scripts.Spells.Abstractions;

/// <summary>
/// Represents a deck of cards that can be drawn from
/// Now uses ResourceManager for card resources to maintain state-driven design
/// </summary>
public class Deck
{
    private List<string> _availableResourceIds;
    private ILogger _logger;

    public Deck()
    {
        _logger = ServiceLocator.GetService<ILogger>();
        RefreshAvailableCards();
    }

    /// <summary>
    /// Refreshes the list of available card resource IDs from the ResourceManager
    /// </summary>
    public void RefreshAvailableCards()
    {
        // Get all resources from ResourceManager through ServiceLocator
        var resourceManager = ServiceLocator.GetService<IResourceManager>();
        if (resourceManager == null)
        {
            _logger?.LogError("ResourceManager not available via ServiceLocator");
            return;
        }
        
        var resources = resourceManager.GetAllSpellCardResources();
        _availableResourceIds = resources.Select(r => r.CardId).ToList();
        
        if (_availableResourceIds.Count == 0)
        {
            _logger?.LogWarning("No spell resources available in the deck!");
        }
    }

    /// <summary>
    /// Gets the next card resource ID from the deck
    /// </summary>
    /// <returns>The resource ID for the next card</returns>
    public string GetNextResourceId()
    {
        if (_availableResourceIds.Count == 0)
        {
            _logger?.LogWarning("Attempted to draw from empty deck!");
            return null;
        }

        var rnd = new Random();
        var index = rnd.Next(_availableResourceIds.Count);
        return _availableResourceIds[index];
    }

    /// <summary>
    /// Gets the next card resource from the deck
    /// </summary>
    /// <returns>The SpellCardResource for the next card</returns>
    public SpellCardResource GetNext()
    {
        var resourceId = GetNextResourceId();
        if (string.IsNullOrEmpty(resourceId))
        {
            return null;
        }
        
        var resourceManager = ServiceLocator.GetService<IResourceManager>();
        if (resourceManager == null)
        {
            _logger?.LogError("ResourceManager not available via ServiceLocator");
            return null;
        }
        
        return resourceManager.GetSpellCardResource(resourceId);
    }
}