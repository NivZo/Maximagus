using Godot;
using System;

public partial class CardSlot : Control, IOrderable
{
    private static readonly string CARD_SCENE = "res://Scenes/Card/CardSlot.tscn";
    private ILogger _logger;

    [Export] public float MaxValidDistance = 512f;

    public Card Card { get; private set; }

    public Vector2 TargetPosition
    {
        get => GlobalPosition;
        set
        {
            GlobalPosition = value;
            Card?.Logic?.SetCardSlot(this);
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

    public override void _Process(double delta)
    {
        base._Process(delta);
        // Original comment preserved: Reordering logic was commented out
        // if (Card.IsDragging)
        // {
        //     ReorderSlots();
        // }
    }

    public void SetCard(Card card)
    {
        try
        {
            Card = card;
            
            if (Card?.Logic != null)
            {
                Card.Logic.SetCardSlot(this);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error setting card for slot", ex);
            throw;
        }
    }

    private static void SwitchSlotContents(CardSlot slotA, CardSlot slotB)
    {
        if (slotA == null || slotB == null) return;
        
        var cardA = slotA.Card;
        slotA.SetCard(slotB.Card);
        slotB.SetCard(cardA);
    }
}