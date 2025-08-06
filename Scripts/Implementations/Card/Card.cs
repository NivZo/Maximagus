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
                
                _logger?.LogInfo($"Card input handler initialized for card {cardId} via notification");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to initialize card input system via notification", ex);
        }
    }
    
    /// <summary>
    /// Creates a card with a specific resource
    /// </summary>
    /// <param name="parent">The parent node to add the card to</param>
    /// <param name="cardSlot">The slot for the card</param>
    /// <param name="resource">The resource for the card</param>
    /// <returns>The created card</returns>
    public static Card Create(Node parent, CardSlot cardSlot, SpellCardResource resource)
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
    
    /// <summary>
    /// Creates a card with a resource from state
    /// </summary>
    /// <param name="parent">The parent node to add the card to</param>
    /// <param name="cardSlot">The slot for the card</param>
    /// <param name="cardState">The card state from the game state</param>
    /// <returns>The created card</returns>
    public static Card CreateFromState(Node parent, CardSlot cardSlot, Scripts.State.CardState cardState)
    {
        try
        {
            parent.ValidateNotNull(nameof(parent));
            cardSlot.ValidateNotNull(nameof(cardSlot));
            cardState.ValidateNotNull(nameof(cardState));
            
            if (string.IsNullOrEmpty(cardState.ResourceId))
            {
                throw new InvalidOperationException("Cannot create card - card state has no resource ID");
            }
            
            // Get resource from ResourceManager
            var resourceManager = Maximagus.Scripts.Managers.ResourceManager.Instance;
            var resource = resourceManager.GetSpellCardResource(cardState.ResourceId);
            
            if (resource == null)
            {
                throw new InvalidOperationException($"Resource not found: {cardState.ResourceId}");
            }
            
            return Create(parent, cardSlot, resource);
        }
        catch (Exception ex)
        {
            ServiceLocator.GetService<ILogger>()?.LogError($"Failed to create card from state: {cardState?.CardId}", ex);
            throw;
        }
    }
}