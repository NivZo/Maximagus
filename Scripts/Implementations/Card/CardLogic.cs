using Godot;
using System;
using System.Linq;
using Scripts.Commands;
using Scripts.Commands.Card;

public partial class CardLogic : Button
{
    private IEventBus _eventBus;
    private IHoverManager _hoverManager;
    private IDragManager _dragManager;
    private ILogger _logger;
    private GameCommandProcessor _commandProcessor;
    private bool _commandSystemReady = false;

    private Vector2 _distanceFromMouse;
    private Vector2 _initialMousePosition;
    private bool _mousePressed = false;
    private const float DRAG_THRESHOLD = 35.0f;

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
        
        // DEFERRED: Don't require GameCommandProcessor during initialization
        // It will be set up when the command system is ready
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
        GuiInput += OnGuiInput;
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
            CheckDragThreshold();
            
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

    private void CheckDragThreshold()
    {
        if (!_mousePressed || IsDragging || _dragManager?.IsDraggingActive == true) 
            return;

        Vector2 currentMousePos = GetGlobalMousePosition();
        float distance = _initialMousePosition.DistanceTo(currentMousePos);
        
        if (distance > DRAG_THRESHOLD)
        {
            _distanceFromMouse = currentMousePos - this.GetCenter();
            StartDragging();
        }
    }

    public void SetCardSlot(CardSlot cardSlot)
    {
        CardSlot = cardSlot?.ValidateNotNull(nameof(cardSlot));
        this.SetCenter(GetTargetCenter());
        InvokePositionChanged();
    }

    private void FollowMouse(float delta)
    {
        Vector2 mousePos = GetGlobalMousePosition();
        this.SetCenter(mousePos - _distanceFromMouse);
        InvokePositionChanged(delta, true);
    }

    private void UpdateVisualPosition(float delta)
    {
        if (IsDragging)
        {
            FollowMouse(delta);
        }
        else if (Card?.Visual != null && Card.Visual.GetCenter() != GetTargetSlottedCenter())
        {
            InvokePositionChanged(delta);
        }
    }

    private void HandleMouseClick(InputEventMouseButton mouseButtonEvent)
    {
        if (mouseButtonEvent.ButtonIndex != MouseButton.Left) return;

        if (mouseButtonEvent.IsPressed())
        {
            HandleMousePressed();
        }
        else
        {
            HandleMouseReleased();
        }
    }

    private void HandleMousePressed()
    {
        if (_dragManager?.IsDraggingActive != true)
        {
            _mousePressed = true;
            _initialMousePosition = GetGlobalMousePosition();
        }
    }

    private void HandleMouseReleased()
    {
        if (IsDragging)
        {
            StopDragging();
        }
        else if (_mousePressed)
        {
            HandleClick();
        }
        
        _mousePressed = false;
    }

    private void HandleClick()
    {
        // Check if command system is ready, otherwise use legacy behavior temporarily
        if (!_commandSystemReady || Card == null || _commandProcessor == null)
        {
            // TEMPORARY FALLBACK: Direct toggle until command system is ready
            IsSelected = !IsSelected;
            this.SetCenter(GetTargetSlottedCenter());
            InvokePositionChanged();
            _logger?.LogWarning($"[CardLogic] Command system not ready, using temporary fallback for card {Card?.GetInstanceId()}");
            return;
        }
        
        try
        {
            var cardId = Card.GetInstanceId().ToString();
            IGameCommand command;
            
            if (IsSelected)
            {
                command = new DeselectCardCommand(cardId);
            }
            else
            {
                command = new SelectCardCommand(cardId);
            }
            
            var success = _commandProcessor.ExecuteCommand(command);
            _logger?.LogInfo($"[CardLogic] Card {cardId} {(IsSelected ? "deselection" : "selection")} command executed: {success}");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error handling card click: {ex.Message}", ex);
        }
        
        // State will be synced automatically in next _Process call via SyncWithGameState()
    }

    private void StartDragging()
    {
        if (_dragManager?.StartDrag(Card) != true) return;

        _collisionShape?.SetDeferred("disabled", false);
        
        _eventBus?.Publish(new CardDragStartedEvent(Card));
    }

    private void StopDragging()
    {
        if (Card == null) return;
        
        var card = Card;
        
        _dragManager?.EndDrag(card);
        _collisionShape?.SetDeferred("disabled", true);

        this.SetCenter(GetTargetSlottedCenter());
        InvokePositionChanged();

        // TODO: Future Phase - Implement ReorderCardsCommand here
        // For now, keep legacy drag behavior
        _eventBus?.Publish(new CardDragEndedEvent(card));
    }

    public void OnGuiInput(InputEvent @event)
    {
        try
        {
            if (@event is InputEventMouseButton mouseButtonEvent)
            {
                HandleMouseClick(mouseButtonEvent);
            }

            if (IsDragging) return;
            if (@event is not InputEventMouseMotion mouseMotion) return;
            
            HandleMouseHover(mouseMotion);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error handling GUI input for {GetParent()?.Name}", ex);
        }
    }

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

    private void HandleMouseHover(InputEventMouseMotion mouseMotion)
    {
        if (_hoverManager?.CurrentlyHoveringCard != Card || _dragManager?.IsDraggingActive == true || IsDragging) return;

        var localPosition = mouseMotion.Position;
        _eventBus?.Publish(new CardMouseMovedEvent(Card, localPosition));
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
