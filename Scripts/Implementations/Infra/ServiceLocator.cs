using System;
using System.Collections.Generic;
using Godot;
using Maximagus.Scripts.Input;
using Maximagus.Scripts.Managers;
using Maximagus.Scripts.Spells.Implementations;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();

    public static T RegisterService<T>(T service)
    {
        _services[typeof(T)] = service;
        return service;
    }

    public static T GetService<T>()
    {
        return _services.TryGetValue(typeof(T), out var service) ? (T)service : default(T);
    }

    public static void Initialize()
    {
        RegisterService<ILogger>(new GodotLogger());
        RegisterService<IEventBus>(new SimpleEventBus());
        RegisterService<IHoverManager>(new HoverManager());
        RegisterService<IDragManager>(new DragManager());
        RegisterService<IHandManager>(new HandManager());
        RegisterService<IStatusEffectManager>(new StatusEffectManager());
        RegisterService<ISpellProcessingManager>(new SpellProcessingManager());

        GD.Print($"Initialized {_services.Count} Services");
    }

    public static void InitializeNodes(Main main)
    {
        main.AddChild(RegisterService(new GameInputManager()));
    }
}