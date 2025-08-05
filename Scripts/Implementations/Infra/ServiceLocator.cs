using System;
using System.Collections.Generic;
using Godot;
using Maximagus.Scripts.Managers;
using Maximagus.Scripts.Spells.Implementations;
using Scripts.State;
using Scripts.Commands;

public static class ServiceLocator
{
    private static Main _main;
    private static readonly Dictionary<Type, Lazy<object>> _services = [];

    public static T GetService<T>()
    {
        return _services.TryGetValue(typeof(T), out var service) ? (T)service.Value : default;
    }

    public static void Initialize(Main main)
    {
        _main = main;

        // Script services
        RegisterService<ILogger, GodotLogger>();
        RegisterService<IEventBus, SimpleEventBus>();
        RegisterService<IHoverManager, HoverManager>();
        RegisterService<IHandManager, HandManager>();
        RegisterService<IGameStateManager, GameStateManager>();
        RegisterService<IStatusEffectManager, StatusEffectManager>();
        RegisterService<ISpellProcessingManager, SpellProcessingManager>();


        // Node services
        RegisterNodeService<QueuedActionsManager>(false);
    }

    public static void RegisterService<T>(T instance)
    {
        _services[typeof(T)] = new Lazy<object>(() => instance);
    }

    private static void RegisterService<TInterface, TImplementation>()
        where TImplementation : TInterface, new()
    {
        var lazyImplementation = new Lazy<object>(() => new TImplementation());
        _services[typeof(TInterface)] = lazyImplementation;
    }

    private static void RegisterMainService<T>(Func<T> factory)
    {
        var lazyService = new Lazy<object>(() => factory());
        _services[typeof(T)] = lazyService;
    }

    private static void RegisterNodeService<TNode>(bool lazy = true)
        where TNode : Node, new()
    {
        var lazyNode = new Lazy<object>(() =>
        {
            var node = new TNode();
            _main.AddChild(node);
            return node;
        });

        if (!lazy)
        {
            GD.Print("Initializing non-lazy node service - ", lazyNode.Value.GetType());
        }

        _services[typeof(TNode)] = lazyNode;
    }
}