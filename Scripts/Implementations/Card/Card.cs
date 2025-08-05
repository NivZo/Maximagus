using Godot;
using Maximagus.Scripts.Spells.Abstractions;
using System;
using Scripts.Input;

public partial class Card : Control
{
    private static readonly string CARD_SCENE = "res://Scenes/Card/Card.tscn";
    private ILogger _logger;

    public CardLogic Logic { get; private set; }
    public CardVisual Visual { get; private set; }
    
    // New input system integration
    private CardInputHandler _cardInputHandler;
    
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
        Visual.SetupCardResource(Resource);
        Logic.Card = this;
        
        // New input system will be initialized via notification from Main
    }

    public override void _Input(InputEvent @event)
    {
        // Forward input events to CardInputHandler if available
        if (_cardInputHandler != null)
        {
            _cardInputHandler._Input(@event);
        }
    }

    /// <summary>
    /// Called by Main when the input system becomes available
    /// </summary>
    public void NotifyInputSystemReady(InputToCommandMapper inputMapper)
    {
        try
        {
            if (_cardInputHandler == null && inputMapper != null)
            {
                // Create and add card input handler now that input mapper is available
                _cardInputHandler = new CardInputHandler();
                AddChild(_cardInputHandler);
                
                // Initialize with card ID and input mapper
                var cardId = GetInstanceId().ToString();
                _cardInputHandler.Initialize(cardId, inputMapper);
                
                // Connect mouse enter/exit events from CardLogic to CardInputHandler
                if (Logic != null)
                {
                    Logic.MouseEntered += _cardInputHandler.OnMouseEntered;
                    Logic.MouseExited += _cardInputHandler.OnMouseExited;
                }
                
                _logger?.LogInfo($"Card input handler initialized for card {cardId} via notification");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to initialize card input system via notification", ex);
        }
    }
    
    public static Card Create(Node parent, CardSlot cardSlot, SpellCardResource resource)
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