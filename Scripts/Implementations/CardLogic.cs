using Godot;
using System;

public partial class CardLogic : Button
{
    private IEventBus _eventBus;
    private IDragManager _dragManager;
    private ILogger _logger;

    private Vector2 _distanceFromMouse;
    private Vector2 _initialMousePosition;
    private bool _mousePressed = false;
    private const float DRAG_THRESHOLD = 35.0f;

    public bool IsSelected { get; private set; } = false;
    public bool IsHovering { get; private set; } = false;
    public bool IsDragging { get; private set; } = false;
    public CardVisual Visual { get; private set; }
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

    public void SetVisual(CardVisual visual)
    {
        if (Visual != null)
            UnsubscribeFromVisualEvents();

        Visual = visual;

        if (Visual != null)
            SubscribeToVisualEvents();
    }

    private void SubscribeToVisualEvents()
    {
        _eventBus?.Subscribe<CardDragStartedEvent>(OnCardDragStarted);
        _eventBus?.Subscribe<CardDragEndedEvent>(OnCardDragEnded);
        _eventBus?.Subscribe<CardHoverStartedEvent>(OnCardHoverStarted);
        _eventBus?.Subscribe<CardHoverEndedEvent>(OnCardHoverEnded);
        _eventBus?.Subscribe<CardClickedEvent>(OnCardClicked);
        _eventBus?.Subscribe<CardPositionChangedEvent>(OnCardPositionChanged);
    }

    private void UnsubscribeFromVisualEvents()
    {
        _eventBus?.Unsubscribe<CardDragStartedEvent>(OnCardDragStarted);
        _eventBus?.Unsubscribe<CardDragEndedEvent>(OnCardDragEnded);
        _eventBus?.Unsubscribe<CardHoverStartedEvent>(OnCardHoverStarted);
        _eventBus?.Unsubscribe<CardHoverEndedEvent>(OnCardHoverEnded);
        _eventBus?.Unsubscribe<CardClickedEvent>(OnCardClicked);
        _eventBus?.Unsubscribe<CardPositionChangedEvent>(OnCardPositionChanged);
    }

    private void OnCardDragStarted(CardDragStartedEvent evt)
    {
        if (evt.Card == GetParent<Card>())
            Visual?.OnDragStarted();
    }

    private void OnCardDragEnded(CardDragEndedEvent evt)
    {
        if (evt.Card == GetParent<Card>())
            Visual?.OnDragEnded();
    }

    private void OnCardHoverStarted(CardHoverStartedEvent evt)
    {
        if (evt.Card == GetParent<Card>())
            Visual?.OnHoverStarted();
    }

    private void OnCardHoverEnded(CardHoverEndedEvent evt)
    {
        if (evt.Card == GetParent<Card>())
            Visual?.OnHoverEnded();
    }

    private void OnCardClicked(CardClickedEvent evt)
    {
        if (evt.Card == GetParent<Card>())
            Visual?.OnClicked();
    }

    private void OnCardPositionChanged(CardPositionChangedEvent evt)
    {
        if (evt.Card == GetParent<Card>())
            Visual?.OnPositionChanged(evt.Delta, evt.Position, evt.IsDueToDragging);
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
        else if (Visual != null && Visual.GetCenter() != GetTargetSlottedCenter())
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
        _eventBus?.Publish(new CardClickedEvent(GetParent<Card>()));
    }

    private void StartDragging()
    {
        var card = GetParent<Card>();
        
        if (!_dragManager.StartDrag(card))
            return;

        IsDragging = true;
        _collisionShape.SetDeferred("disabled", false);
        
        _eventBus?.Publish(new CardDragStartedEvent(card));
    }

    private void StopDragging()
    {
        var card = GetParent<Card>();
        
        _dragManager.EndDrag(card);
        IsDragging = false;
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
            if (@event is not InputEventMouseMotion) return;
            
            HandleMouseHover();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error handling GUI input for {GetParent()?.Name}", ex);
        }
    }

    public void OnMouseEntered()
    {
        if (_dragManager.IsDraggingActive || IsDragging) return;

        IsHovering = true;
        _eventBus?.Publish(new CardHoverStartedEvent(GetParent<Card>()));
    }

    public void OnMouseExited()
    {
        IsHovering = false;
        _eventBus?.Publish(new CardHoverEndedEvent(GetParent<Card>()));
    }

    private void HandleMouseHover()
    {
        // Mouse movement handling can be added here if needed
        // For now, maintaining original functionality
    }

    private Vector2 GetTargetCenter()
    {
        return IsDragging ? GetTargetFollowMouseCenter() : GetTargetSlottedCenter();
    }

    private Vector2 GetTargetSlottedCenter()
    {
        if (CardSlot == null) return Vector2.Zero;
        
        Vector2 offset = IsSelected && Visual != null ? new Vector2(0, Visual.SelectionVerticalOffset) : Vector2.Zero;
        return CardSlot.GetCenter() + offset;
    }

    private Vector2 GetTargetFollowMouseCenter()
    {
        return this.GetCenter();
    }

    public void InvokePositionChanged(float? delta = null, bool isDueToDragging = false)
    {
        var card = GetParent<Card>();
        var actualDelta = delta ?? (float)GetProcessDeltaTime();
        
        _eventBus?.Publish(new CardPositionChangedEvent(card, actualDelta, GetTargetCenter(), isDueToDragging));
    }

    public void DestroyCard()
    {
        Visual?.StartDestroyAnimation();
    }

    public override void _ExitTree()
    {
        try
        {
            UnsubscribeFromVisualEvents();
            base._ExitTree();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error during CardLogic cleanup for {GetParent()?.Name}", ex);
        }
    }
}