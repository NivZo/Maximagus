using System.Collections;
using System.Collections.Generic;
using Maximagus.Scripts.Managers;
using Maximagus.Scripts.Spells.Abstractions;

public class Deck
{
    private IResourceManager _resourceManager;
    private ILogger _logger;

    private int _maxSize;
    private Queue<SpellCardResource> _deckQueue = new();

    public Deck(int maxSize = 20)
    {
        _logger = ServiceLocator.GetService<ILogger>();
        _resourceManager = ServiceLocator.GetService<IResourceManager>();

        _maxSize = maxSize;

        for (int i = 0; i < _maxSize; i++)
        {
            var resource = _resourceManager.GetNextSpellCardResource();
            _deckQueue.Enqueue(resource);
        }
    }
    public SpellCardResource GetNext()
    {
        if (_deckQueue.Count == 0)
        {
            _logger?.LogInfo("Deck is empty, reshuffling discard pile into deck");
            return null;
        }

        else
        {
            return _deckQueue.Dequeue();
        }
    }
}