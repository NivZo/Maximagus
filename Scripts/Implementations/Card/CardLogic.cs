using Godot;
using System;
using System.Linq;
using Scripts.Commands;

public partial class CardLogic : Button
{
    private IEventBus _eventBus;
    private IHoverManager _hoverManager;
    private IDragManager _dragManager;
    private ILogger _logger;
    private GameCommandProcessor _commandProcessor;
    private bool _commandSystemReady = false;

    public bool IsSelected { get; private set; } = false;
    public bool IsHovering => _hoverManager != null ? _hoverManager.CurrentlyHoveringCard == Card : false;
    public bool IsDragging => _dragManager != null ? _dragManager.CurrentlyDraggingCard == Card : false;
    public Card Card { get; set; }
    public CardSlot CardSlot { get; private set; }

    private Area2D _interactionArea;
    private CollisionShape2D _collisionShape;

    public override void _Ready()
    {
        try
        {
            SetupServices();
            InitializeComponents();
            SetupEventHandlers();
            SetupCollision();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error initializing CardLogic for {GetParent()?.Name}", ex);
            throw;
        }
    }

    private void SetupServices()
    {
        _logger = ServiceLocator.GetService<ILogger>();
        _eventBus = ServiceLocator.GetService<IEventBus>();
        _hoverManager = ServiceLocator.GetService<IHoverManager>();
        _dragManager = ServiceLocator.GetService<IDragManager>();
        
        // DEFERRED: Set up command system when available
        TrySetupCommandSystem();
    }

    private void TrySetupCommandSystem()
    {
        if (_commandSystemReady) return;
        
        _commandProcessor = ServiceLocator.GetService<GameCommandProcessor>();
        if (_commandProcessor != null)
        {
            _commandSystemReady = true;
            _logger?.LogInfo($"[CardLogic] Command system ready for card {Card?.GetInstanceId()}");
        }
    }

    private void InitializeComponents()
    {
        _interactionArea = GetNode<Area2D>("InteractionArea").ValidateNotNull("InteractionArea");
        _collisionShape = _interactionArea.GetNode<CollisionShape2D>("CollisionShape2D").ValidateNotNull("CollisionShape2D");
    }

    private void SetupEventHandlers()
    {
        // REMOVED: No direct input handling - CardInputHandler handles all input
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        // Subscribe to hand events through event bus
        _eventBus?.Subscribe<HandCardSlotsChangedEvent>(_ => InvokePositionChanged());
    }

    private void SetupCollision()
    {
        _collisionShape.SetDeferred("disabled", true);
        
        if (_collisionShape.Shape is RectangleShape2D rect)
            rect.Size = Size;
        
        _interactionArea.Position = Size / 2f;
    }

    public override void _Process(double delta)
    {
        try
        {
            // Try to set up command system if not ready yet
            if (!_commandSystemReady)
            {
                TrySetupCommandSystem();
            }
            
            UpdateVisualPosition((float)delta);
            
            // Only sync with GameState if command system is ready
            if (_commandSystemReady)
            {
                SyncWithGameState();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error in CardLogic process for {GetParent()?.Name}", ex);
        }
    }

    private void SyncWithGameState()
    {
        // Keep CardLogic selection state in sync with GameState
        if (_commandProcessor?.CurrentState == null || Card == null) return;
        
        try
        {
            var currentState = _commandProcessor.CurrentState;
            var cardId = Card.GetInstanceId().ToString();
            var shouldBeSelected = currentState.Hand.SelectedCardIds.Any(id => id == cardId);
            
            if (IsSelected != shouldBeSelected)
            {
                IsSelected = shouldBeSelected;
                this.SetCenter(GetTargetSlottedCenter());
                InvokePositionChanged();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error syncing with GameState: {ex.Message}", ex);
        }
    }

    public void SetCardSlot(CardSlot cardSlot)
    {
        CardSlot = cardSlot?.ValidateNotNull(nameof(cardSlot));
        this.SetCenter(GetTargetCenter());
        InvokePositionChanged();
    }

    private void UpdateVisualPosition(float delta)
    {
        if (IsDragging)
        {
            // REMOVED: FollowMouse logic - CardInputHandler and commands handle drag
            // Visual positioning during drag is handled by the drag system
        }
        else if (Card?.Visual != null && Card.Visual.GetCenter() != GetTargetSlottedCenter())
        {
            InvokePositionChanged(delta);
        }
    }

    // REMOVED: All mouse input handling - CardInputHandler handles input routing
    // REMOVED: All drag threshold detection - CardInputHandler handles drag detection
    // REMOVED: All click handling - InputToCommandMapper handles click to command conversion

    public void OnMouseEntered()
    {
        if (_hoverManager?.IsHoveringActive == true || _dragManager?.IsDraggingActive == true) return;
        if (_hoverManager?.StartHover(Card) != true) return;

        _eventBus?.Publish(new CardHoverStartedEvent(Card));
    }

    public void OnMouseExited()
    {
        if (_hoverManager?.CurrentlyHoveringCard != Card || _dragManager?.IsDraggingActive == true) return;

        _hoverManager?.EndHover(Card);

        _eventBus?.Publish(new CardHoverEndedEvent(Card));
    }

    private Vector2 GetTargetCenter()
    {
        return IsDragging ? GetTargetFollowMouseCenter() : GetTargetSlottedCenter();
    }

    private Vector2 GetTargetSlottedCenter()
    {
        if (CardSlot == null) return Vector2.Zero;
        
        Vector2 offset = Vector2.Zero;
        if (IsSelected && Card?.Visual != null)
        {
            try
            {
                offset = new Vector2(0, Card.Visual.SelectionVerticalOffset);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error accessing Card.Visual.SelectionVerticalOffset: {ex.Message}", ex);
                // Continue with zero offset
            }
        }
        
        return CardSlot.GetCenter() + offset;
    }

    private Vector2 GetTargetFollowMouseCenter()
    {
        return this.GetCenter();
    }

    private void InvokePositionChanged(float? delta = null, bool isDueToDragging = false)
    {
        if (Card == null) return;
        
        try
        {
            var card = Card;
            var actualDelta = delta ?? (float)GetProcessDeltaTime();
            
            _eventBus?.Publish(new CardPositionChangedEvent(card, actualDelta, GetTargetCenter(), isDueToDragging));
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error invoking position changed: {ex.Message}", ex);
        }
    }

    public override void _ExitTree()
    {
        try
        {
            base._ExitTree();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error during CardLogic cleanup for {GetParent()?.Name}", ex);
        }
    }
}
