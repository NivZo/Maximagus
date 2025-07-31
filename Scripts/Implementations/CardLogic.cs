using Godot;
using System;

public partial class CardLogic : Button
{
    private IEventBus _eventBus;
    private IHoverManager _hoverManager;
    private IDragManager _dragManager;
    private ILogger _logger;

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
            UpdateVisualPosition((float)delta);
            CheckDragThreshold();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error in CardLogic process for {GetParent()?.Name}", ex);
        }
    }

    private void CheckDragThreshold()
    {
        if (!_mousePressed || IsDragging || _dragManager.IsDraggingActive) 
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
        else if (Card.Visual != null && Card.Visual.GetCenter() != GetTargetSlottedCenter())
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
        if (!_dragManager.IsDraggingActive)
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
        IsSelected = !IsSelected;
        this.SetCenter(GetTargetSlottedCenter());
        
        InvokePositionChanged();
        _eventBus?.Publish(new CardClickedEvent(Card));
    }

    private void StartDragging()
    {
        if (!_dragManager.StartDrag(Card)) return;

        _collisionShape.SetDeferred("disabled", false);
        
        _eventBus?.Publish(new CardDragStartedEvent(Card));
    }

    private void StopDragging()
    {
        var card = Card;
        
        _dragManager.EndDrag(card);
        _collisionShape.SetDeferred("disabled", true);

        this.SetCenter(GetTargetSlottedCenter());
        InvokePositionChanged();

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
        if (_hoverManager.IsHoveringActive || _dragManager.IsDraggingActive || IsDragging) return;
        if (!_hoverManager.StartHover(Card)) return;

        _eventBus?.Publish(new CardHoverStartedEvent(Card));
    }

    public void OnMouseExited()
    {
        if (IsDragging) return;

        _hoverManager.EndHover(Card);

        _eventBus?.Publish(new CardHoverEndedEvent(Card));
    }

    private void HandleMouseHover(InputEventMouseMotion mouseMotion)
    {
        if (_hoverManager.CurrentlyHoveringCard != Card || _dragManager.IsDraggingActive || IsDragging) return;

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
        
        Vector2 offset = IsSelected && Card.Visual != null ? new Vector2(0, Card.Visual.SelectionVerticalOffset) : Vector2.Zero;
        return CardSlot.GetCenter() + offset;
    }

    private Vector2 GetTargetFollowMouseCenter()
    {
        return this.GetCenter();
    }

    public void InvokePositionChanged(float? delta = null, bool isDueToDragging = false)
    {
        var card = Card;
        var actualDelta = delta ?? (float)GetProcessDeltaTime();
        
        _eventBus?.Publish(new CardPositionChangedEvent(card, actualDelta, GetTargetCenter(), isDueToDragging));
    }

    public void DestroyCard()
    {
        var card = Card;
        _eventBus?.Publish(new CardDestroyStartedEvent(card));
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