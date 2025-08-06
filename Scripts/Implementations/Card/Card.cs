using Godot;
using Maximagus.Scripts.Spells.Abstractions;
using System;
using Scripts.Input;

public partial class Card : Control
{
    private static readonly string CARD_SCENE = "res://Scenes/Card/Card.tscn";
    private ILogger _logger;

    public string CardId { get; private set; }
    public CardLogic Logic { get; private set; }
    public CardVisual Visual { get; private set; }
    
    public SpellCardResource Resource { get; private set; }

    public bool IsSelected => Logic?.IsSelected ?? false;
    public bool IsDragging => Logic?.IsDragging ?? false;
    public bool IsHovering => Logic?.IsHovering ?? false;

    public override void _Ready()
    {
        try
        {
            base._Ready();

            _logger = ServiceLocator.GetService<ILogger>();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error initializing card {Name}", ex);
            throw;
        }
    }
    
    public static Card Create(Node parent, CardSlot cardSlot, SpellCardResource resource, string cardId)
    {
        try
        {
            parent.ValidateNotNull(nameof(parent));
            cardSlot.ValidateNotNull(nameof(cardSlot));
            resource.ValidateNotNull(nameof(resource));

            var scene = GD.Load<PackedScene>(CARD_SCENE);
            if (scene == null)
                throw new InvalidOperationException($"Could not load card scene: {CARD_SCENE}");

            var card = scene.Instantiate<Card>();
            if (card == null)
                throw new InvalidOperationException("Failed to instantiate card from scene");

            parent.AddChild(card);
            card.Resource = resource;
            card.CardId = cardId;
            card.Logic = card.GetNode<CardLogic>("CardLogic").ValidateNotNull(nameof(card.Logic));
            card.Logic.Card = card;
            card.Visual = card.GetNode<CardVisual>("CardVisual").ValidateNotNull(nameof(card.Visual));
            card.Visual.SetupCardResource(resource);
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