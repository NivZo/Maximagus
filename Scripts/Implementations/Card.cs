using Godot;
using System;

public partial class Card : Control
{
    private static readonly string CARD_SCENE = "res://Scenes/Card/Card.tscn";
    private ILogger _logger;

    public CardLogic CardLogic { get; private set; }
    public CardVisual CardVisual { get; private set; }

    public bool IsSelected => CardLogic?.IsSelected ?? false;
    public bool IsDragging => CardLogic?.IsDragging ?? false;
    public bool IsHovering => CardLogic?.IsHovering ?? false;

    public override void _Ready()
    {
        try
        {
            base._Ready();

            _logger = ServiceLocator.GetService<ILogger>();

            InitializeCard();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error initializing card {Name}", ex);
            throw;
        }
    }

    private void InitializeCard()
    {
        GlobalPosition = Vector2.Zero;

        CardLogic = GetNode<CardLogic>("CardLogic").ValidateNotNull(nameof(CardLogic));
        CardVisual = GetNode<CardVisual>("CardVisual").ValidateNotNull(nameof(CardVisual));
        
        CardLogic.SetVisual(CardVisual);

        // TEMP - keeping original functionality
        var label = CardVisual.GetNode<Label>("Label");
        if (label != null)
            label.Text = Name;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public static Card Create(Node parent, CardSlot cardSlot)
    {
        try
        {
            parent.ValidateNotNull(nameof(parent));
            cardSlot.ValidateNotNull(nameof(cardSlot));

            var scene = GD.Load<PackedScene>(CARD_SCENE);
            if (scene == null)
                throw new InvalidOperationException($"Could not load card scene: {CARD_SCENE}");

            var card = scene.Instantiate<Card>();
            if (card == null)
                throw new InvalidOperationException("Failed to instantiate card from scene");

            parent.AddChild(card);
            cardSlot.SetCard(card);
            
            return card;
        }
        catch (Exception ex)
        {
            ServiceLocator.GetService<ILogger>()?.LogError("Failed to create card", ex);
            throw;
        }
    }
}