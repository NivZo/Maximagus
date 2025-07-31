using System;
using System.Collections.Generic;
using Godot;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();
    
    public static void RegisterService<T>(T service)
    {
        _services[typeof(T)] = service;
    }
    
    public static T GetService<T>()
    {
        return _services.TryGetValue(typeof(T), out var service) ? (T)service : default(T);
    }

    public static void Initialize()
    {
        RegisterService<ILogger>(new GodotLogger());
        RegisterService<IEventBus>(new SimpleEventBus());
        RegisterService<IHoverManager>(new HoverManager(GetService<ILogger>()));
        RegisterService<IDragManager>(new DragManager(GetService<ILogger>()));

        GD.Print($"Initialized {_services.Count} Services");
    }
}