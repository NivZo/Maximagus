using System;
using System.Collections.Generic;
using System.Linq;

public class SimpleEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    
    public void Subscribe<T>(Action<T> handler) where T : class
    {
        var eventType = typeof(T);
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<Delegate>();
        
        _handlers[eventType].Add(handler);
    }
    
    public void Unsubscribe<T>(Action<T> handler) where T : class
    {
        var eventType = typeof(T);
        if (_handlers.ContainsKey(eventType))
            _handlers[eventType].Remove(handler);
    }
    
    public void Publish<T>(T eventData) where T : class
    {
        var eventType = typeof(T);
        if (_handlers.ContainsKey(eventType))
        {
            foreach (var handler in _handlers[eventType].Cast<Action<T>>())
            {
                try
                {
                    handler(eventData);
                }
                catch (Exception ex)
                {
                    ServiceLocator.GetService<ILogger>()?.LogError($"Error handling event {eventType.Name}", ex);
                }
            }
        }
    }
}