using Godot;
using System;

public partial class Card : Control
{
    private static readonly string CARD_SCENE = "res://Scenes/Card/Card.tscn";
    private ILogger _logger;

    public CardLogic Logic { get; private set; }
    public CardVisual Visual { get; private set; }
    
    public CardResource Resource { get; private set; }

    public bool IsSelected => Logic?.IsSelected ?? false;
    public bool IsDragging => Logic?.IsDragging ?? false;
    public bool IsHovering => Logic?.IsHovering ?? false;

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

        Logic = GetNode<CardLogic>("CardLogic").ValidateNotNull(nameof(Logic));
        Visual = GetNode<CardVisual>("CardVisual").ValidateNotNull(nameof(Visual));
        Logic.Card = this;        

        // TEMP - keeping original functionality
        var label = Visual.GetNode<Label>("Label");
        if (label != null)
            label.Text = Resource.Value.ToString();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
    }

    public static Card Create(Node parent, CardSlot cardSlot, CardResource resource)
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

            card.Resource = resource;
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