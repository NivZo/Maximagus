using Godot;
using System;

public partial class CardSlot : Control, IOrderable
{
    private static readonly string CARD_SCENE = "res://Scenes/Card/CardSlot.tscn";

    [Export] public float MaxValidDistance = 512f;

    private ILogger _logger;

    public Vector2 TargetPosition
    {
        get => GlobalPosition;
        set
        {
            GlobalPosition = value;
        }
    }

    public Vector2 Weight => Vector2.One;

    public static CardSlot Create(Node parent = null)
    {
        try
        {
            var scene = GD.Load<PackedScene>(CARD_SCENE);
            if (scene == null)
                throw new InvalidOperationException($"Could not load card slot scene: {CARD_SCENE}");

            var cardSlot = scene.Instantiate<CardSlot>();
            if (cardSlot == null)
                throw new InvalidOperationException("Failed to instantiate card slot from scene");
            parent?.AddChild(cardSlot);
            return cardSlot;
        }
        catch (Exception ex)
        {
            ServiceLocator.GetService<ILogger>()?.LogError("Failed to create card slot", ex);
            throw;
        }
    }

    public override void _Ready()
    {
        try
        {
            base._Ready();
            _logger = ServiceLocator.GetService<ILogger>();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error initializing CardSlot", ex);
            throw;
        }
    }
}