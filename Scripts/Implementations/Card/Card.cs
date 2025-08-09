using Godot;
using Maximagus.Scripts.Spells.Abstractions;
using System;
using Scripts.Commands.Card;
using Scripts.Commands;
using Scripts.Config;
using System.Linq;
using System.Collections.Generic;

public partial class Card : Control, IOrderable
{
    private static readonly string CARD_SCENE = "res://Scenes/Card/Card.tscn";

    [ExportGroup("Visual Settings")]
    [Export] public float AngleXMax = 15.0f;
    [Export] public float AngleYMax = 15.0f;
    [Export] public float AngleIdleRatio = 0.15f;
    [Export] public float MaxOffsetShadow = 50.0f;
    [Export] public float SelectionVerticalOffset = -64.0f;

    [ExportGroup("Animation Settings")]
    [Export] public float HoverScale = 1.1f;
    [Export] public float DragScale = 1.3f;
    [Export] public float ClickAnimationDuration = 1f;
    [Export] public float HoverAnimationDuration = 0.5f;
    [Export] public float RotationResetDuration = 0.5f;
    [Export] public float DragRotationResetDuration = 0.3f;
    [Export] public float ScaleResetDuration = 0.55f;
    [Export] public float DestroyDuration = 2.0f;
    [Export] public float ShadowFadeDuration = 1.0f;

    [ExportGroup("Movement Physics")]
    [Export(PropertyHint.Range, "0, 100, 1")] public float MoveSpeedFactor = 5f;
    [Export(PropertyHint.Range, "0, 100, 1")] public float DragMoveSpeedFactor = 8f;
    
    [ExportGroup("Sway Physics")]
    [Export(PropertyHint.Range, "0, 500, 1")] public float Stiffness { get; set; } = 150f;
    [Export(PropertyHint.Range, "0, 50, 0.1")] public float Damping { get; set; } = 10f;
    [Export(PropertyHint.Range, "0, 0.01, 0.0001")] public float VelocityFactor { get; set; } = 0.0015f;
    [Export(PropertyHint.Range, "0, 90, 1")] public float MaxRotationDegrees { get; set; } = 15f;

    private ILogger _logger;
    private IHoverManager _hoverManager;
    private IGameCommandProcessor _commandProcessor;

    public string CardId { get; private set; }
    public SpellCardResource Resource { get; private set; }

    private Control _textures;
    private TextureRect _cardTexture;
    private TextureRect _artTexture;
    private TextureRect _shadowTexture;
    private Tooltip _tooltip;

    private bool _isSelected => GetCardStateFromGameState()?.IsSelected ?? false;
    private bool _isDragging => GetCardStateFromGameState()?.IsDragging ?? false;
    private bool _isHovering => _hoverManager?.CurrentlyHoveringCard == this;

    private Vector2 _distanceFromMouse;
    private Vector2 _initialMousePosition;
    private bool _mousePressed = false;
    private const float DRAG_THRESHOLD = GameConfig.DRAG_THRESHOLD;

    private bool _requiresPerspectiveReset = false;
    private float _idleAnimationTime = 0;
    private float _angularVelocity = 0f;
    private Vector2 _lastPosition;
    private Vector2 _velocity;
    private Vector2 _shadowBasePosition;
    private readonly Dictionary<string, Tween> _propertyTweens = new();

    private const string SHADER_X_ROT_PROPERTY = "shader_x_rot";
    private const string SHADER_Y_ROT_PROPERTY = "shader_y_rot";

    public Vector2 TargetPosition { get; set; }
    public Vector2 Weight => Vector2.One;

    #region Lifecycle Methods
    public override void _Ready()
    {
        try
        {
            base._Ready();
            SetupServices();
            InitializeComponents();
            SetupEventHandlers();
            SetupVisualSettings();
            SetupCardResource(Resource);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error initializing card {Name}", ex);
            throw;
        }
    }

    public override void _Process(double delta)
    {
        try
        {
            if (!Visible) return;
            
            SyncWithGameState();
            HandleMovementToTarget((float)delta);
            UpdateVisualEffects((float)delta);
            
            if (_mousePressed && !_isDragging)
            {
                CheckDragThreshold();
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error in Card process for {Name}", ex);
        }
    }

    public override void _ExitTree()
    {
        try
        {
            KillAllTweens();
            base._ExitTree();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error during Card cleanup for {Name}", ex);
        }
    }
    #endregion

    #region Initialization
    private void SetupServices()
    {
        _logger = ServiceLocator.GetService<ILogger>();
        _hoverManager = ServiceLocator.GetService<IHoverManager>();
        _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
    }

    private void InitializeComponents()
    {
        _textures = GetNode<Control>("Textures").ValidateNotNull("Textures");
        _cardTexture = _textures.GetNode<TextureRect>("Card").ValidateNotNull("Card");
        _artTexture = _cardTexture.GetNode<TextureRect>("Art").ValidateNotNull("Art");
        _shadowTexture = _cardTexture.GetNode<TextureRect>("Shadow").ValidateNotNull("Shadow");
        _shadowBasePosition = _shadowTexture.Position;

        Resized += SetupPerspectiveRectSize;
        SetupPerspectiveRectSize();
    }

    private void SetupEventHandlers()
    {
        // GUI Input Events
        GuiInput += OnGuiInput;
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private void SetupVisualSettings()
    {
        _idleAnimationTime += (float)new Random().NextDouble() * 20 - 10;
        AngleXMax = Mathf.DegToRad(AngleXMax);
        AngleYMax = Mathf.DegToRad(AngleYMax);
        
        PivotOffset = Size / 2;
        _textures.PivotOffset = Size / 2;
    }

    #endregion

    #region Static Creation Method
    public static Card Create(SpellCardResource resource, string cardId)
    {
        try
        {
            resource.ValidateNotNull(nameof(resource));

            var scene = GD.Load<PackedScene>(CARD_SCENE);
            if (scene == null)
                throw new InvalidOperationException($"Could not load card scene: {CARD_SCENE}");

            var card = scene.Instantiate<Card>();
            if (card == null)
                throw new InvalidOperationException("Failed to instantiate card from scene");

            card.Resource = resource;
            card.CardId = cardId;

            return card;
        }
        catch (Exception ex)
        {
            ServiceLocator.GetService<ILogger>()?.LogError("Failed to create card", ex);
            throw;
        }
    }
    #endregion

    #region Resource Setup
    private void SetupCardResource(SpellCardResource spellCardResource)
    {
        Resource = spellCardResource;
        _artTexture.Texture = spellCardResource.CardArt;
        _tooltip = Tooltip.Create(this, new(0, -Size.Y), spellCardResource.CardName, spellCardResource.CardDescription);
    }
    #endregion

    #region State Management
    private Scripts.State.CardState GetCardStateFromGameState()
    {
        return _commandProcessor?.CurrentState?.Hand?.Cards?.FirstOrDefault(c => c.CardId == CardId);
    }

    private void SyncWithGameState()
    {
        try
        {
            var cardState = GetCardStateFromGameState();
            if (cardState == null)
            {
                QueueFree();
                return;
            }

            if (_tooltip.Visible && _commandProcessor.CurrentState.Hand.HasDraggingCard && !_isDragging)
            {
                OnHoverEnded();
            }

            // ZIndex = GetCardStateFromGameState()?.Position ?? LayerIndices.Base;
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error syncing with GameState: {ex.Message}", ex);
        }
    }
    #endregion

    #region Input Handling (Non-State Affecting)
    private void OnGuiInput(InputEvent @event)
    {
        try
        {
            if (@event is InputEventMouseButton mouseButtonEvent)
            {
                HandleMouseClick(mouseButtonEvent);
            }

            if (_isDragging) return;
            if (@event is not InputEventMouseMotion mouseMotion) return;
            
            HandleMouseHover(mouseMotion);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error handling GUI input for {Name}", ex);
        }
    }

    private void OnMouseEntered()
    {
        var hasDraggingCard = _commandProcessor.CurrentState.Hand.HasDraggingCard == true;
        if (_hoverManager?.IsHoveringActive == true || hasDraggingCard) return;
        if (_hoverManager?.StartHover(this) != true) return;

        // Visual-only effect: Start hover animations
        OnHoverStarted();
    }

    private void OnMouseExited()
    {
        var hasDraggingCard = _commandProcessor.CurrentState.Hand.HasDraggingCard == true;
        
        if (_hoverManager?.CurrentlyHoveringCard != this || hasDraggingCard) return;

        _hoverManager?.EndHover(this);

        // Visual-only effect: End hover animations
        OnHoverEnded();
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
        _mousePressed = true;
        _initialMousePosition = GetGlobalMousePosition();
    }

    private void HandleMouseReleased()
    {
        if (_isDragging)
        {
            SendDragEndCommand();
        }
        else if (_mousePressed)
        {
            HandleClick();
        }
        
        _mousePressed = false;
    }

    private void HandleMouseHover(InputEventMouseMotion mouseMotion)
    {
        var hasDraggingCard = _commandProcessor.CurrentState.Hand.HasDraggingCard == true;
        
        if (_hoverManager?.CurrentlyHoveringCard != this || hasDraggingCard || _isDragging) return;

        var localPosition = mouseMotion.Position;
        OnMouseMoved(localPosition);
    }
    #endregion

    #region Game Commands (State Affecting)
    private void HandleClick()
    {
        var isSelected = GetCardStateFromGameState()?.IsSelected ?? false;
        GameCommand command;
        if (isSelected)
        {
            command = new DeselectCardCommand(CardId);
        }
        else
        {
            command = new SelectCardCommand(CardId);
        }
        
        _commandProcessor.ExecuteCommand(command);
    }

    private void CheckDragThreshold()
    {
        if (!_mousePressed || _isDragging) 
            return;
        
        if (_commandProcessor.CurrentState.Hand.HasDraggingCard == true)
            return;

        Vector2 currentMousePos = GetGlobalMousePosition();
        float distance = _initialMousePosition.DistanceTo(currentMousePos);
        
        if (distance > DRAG_THRESHOLD)
        {
            _distanceFromMouse = currentMousePos - this.GetCenter();
            SendDragStartCommand();
        }
    }

    private void SendDragStartCommand()
    {
        var command = new StartDragCommand(CardId);
        var success = _commandProcessor.ExecuteCommand(command);
        
        if (success)
        {
            OnDragStarted();
        }
    }

    private void SendDragEndCommand()
    {
        var command = new EndDragCommand(CardId);
        var success = _commandProcessor.ExecuteCommand(command);
        
        if (success)
        {
            OnDragEnded();
        }
    }
    #endregion

    #region Movement and Position Handling
    private void HandleMovementToTarget(float delta)
    {
        MoveToTargetPosition(delta);
    }

    private void MoveToTargetPosition(float delta)
    {
        var currentCenter = this.GetCenter();
        var offset = Size / 2 * (Vector2.One - Scale);
        var targetCenter = GetTargetCenter() + offset;
        
        if (currentCenter != targetCenter)
        {
            float lerpSpeed = _isDragging ? DragMoveSpeedFactor : MoveSpeedFactor;
            var newCenter = currentCenter.Lerp(targetCenter, delta * lerpSpeed);
            newCenter = newCenter.Clamp(currentCenter - Size * 2, currentCenter + Size * 2);
            this.SetCenter(newCenter);

            // Update velocity for sway physics
            if (delta > 0)
            {
                _velocity = (newCenter - _lastPosition) / delta;
            }
            _lastPosition = newCenter;
        }
    }

    private Vector2 GetTargetCenter()
    {
        return _isDragging ? GetTargetFollowMouseCenter() : GetTargetSlottedCenter();
    }

    private Vector2 GetTargetFollowMouseCenter()
    {
        Vector2 mousePos = GetGlobalMousePosition();
        return mousePos - _distanceFromMouse;
    }

    private Vector2 GetTargetSlottedCenter()
    {
        Vector2 offset = Vector2.Zero;
        if (_isSelected)
        {
            try
            {
                offset = new Vector2(0, SelectionVerticalOffset);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error accessing SelectionVerticalOffset: {ex.Message}", ex);
            }
        }
        
        // TargetPosition is now set directly by OrderedContainer
        var targetCenter = TargetPosition + offset;
        return targetCenter;
    }

    #endregion

    #region Visual Effects (Non-State Affecting)
    private void UpdateVisualEffects(float delta)
    {
        UpdateShadow(delta);
        UpdateSway(delta);
        UpdateCardPerspectiveIdle(delta);
    }

    private void UpdateShadow(float delta)
    {
        Vector2 screenCenter = GetViewportRect().GetCenter();
        float distance = this.GetCenter().X - screenCenter.X;

        _shadowTexture.Position = new Vector2(
            Mathf.Lerp(0.0f, -Mathf.Sign(distance) * MaxOffsetShadow, Mathf.Abs(distance / screenCenter.X)),
            _shadowBasePosition.Y + (_isDragging ? Size.Y * .2f : 0)
        );
        _shadowTexture.ZIndex = _isDragging || _isHovering ? 0 : -2;
    }

    private void UpdateSway(float delta)
    {
        float maxRotationRad = Mathf.DegToRad(MaxRotationDegrees);
        var targetRotation = _velocity.X * VelocityFactor;
        targetRotation = Mathf.Clamp(targetRotation, -maxRotationRad, maxRotationRad);

        float springForce = -Stiffness * (_textures.Rotation - targetRotation);
        float dampingForce = -Damping * _angularVelocity;
        float angularAcceleration = springForce + dampingForce;

        _angularVelocity += angularAcceleration * delta;
        _textures.Rotation += _angularVelocity * delta;
    }

    private void UpdateCardPerspectiveIdle(float delta)
    {
        if (_isHovering && !_isDragging) return;
        if (_requiresPerspectiveReset) return;
        
        float sine = Mathf.Sin(_idleAnimationTime);
        float cosine = Mathf.Cos(_idleAnimationTime) - 1;
        float rotX = Mathf.RadToDeg(Mathf.LerpAngle(0, AngleXMax * AngleIdleRatio, sine)) - AngleXMax * AngleIdleRatio * .5f;
        float rotY = Mathf.RadToDeg(Mathf.LerpAngle(0, -AngleYMax * AngleIdleRatio, cosine)) + AngleYMax * AngleIdleRatio * .5f;

        var shader = _cardTexture.Material as ShaderMaterial;
        shader?.SetShaderParameter("x_rot", rotY);
        shader?.SetShaderParameter("y_rot", rotX);
        _idleAnimationTime += delta;
    }

    private void OnMouseMoved(Vector2 mousePosition)
    {
        if (_isDragging) return;

        Vector2 normalizedPosition = new Vector2(
            mousePosition.X / Size.X,
            mousePosition.Y / Size.Y
        );
        UpdateCardPerspective(normalizedPosition);
    }

    private void UpdateCardPerspective(Vector2 normalizedPos)
    {
        normalizedPos = normalizedPos.Clamp(Vector2.Zero, Vector2.One);

        float rotX = Mathf.RadToDeg(Mathf.LerpAngle(-AngleXMax, AngleXMax, normalizedPos.X));
        float rotY = Mathf.RadToDeg(Mathf.LerpAngle(AngleYMax, -AngleYMax, normalizedPos.Y));

        if (_cardTexture.Material is ShaderMaterial shaderMaterial)
        {
            shaderMaterial.SetShaderParameter("x_rot", rotY);
            shaderMaterial.SetShaderParameter("y_rot", rotX);
        }
    }

    private void SetupPerspectiveRectSize()
    {
        if (_cardTexture?.Material is ShaderMaterial shaderMaterial)
        {
            shaderMaterial.SetShaderParameter("rect_size", Size);
        }
    }
    #endregion

    #region Visual State Handlers

    private void OnDragEnded()
    {
        OnHoverEnded();
    }

    private void OnHoverStarted()
    {
        AnimationUtils.AnimateScale(this, HoverScale, HoverAnimationDuration, Tween.TransitionType.Elastic);
        _tooltip?.ShowTooltip();
    }

    private void OnHoverEnded()
    {
        if (_isDragging) return;

        ZIndex = GetCardStateFromGameState()?.Position ?? LayerIndices.Base;
        ResetPerspective();
        ResetScale();
        _tooltip?.HideTooltip();
    }

    private void OnDragStarted()
    {
        KillAllTweens();
        ResetPerspective();
        
        // Update pivot offset to center for scaling
        PivotOffset = Size / 2;
        _textures.PivotOffset = Size / 2;
        
        AnimationUtils.AnimateScale(this, DragScale, HoverAnimationDuration, Tween.TransitionType.Elastic);

        _lastPosition = GlobalPosition;
        _tooltip?.HideTooltip();
    }

    private void ResetPerspective()
    {
        if (_cardTexture?.Material is ShaderMaterial shaderMaterial)
        {
            var xRotTween = CreatePropertyTween(SHADER_X_ROT_PROPERTY)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            xRotTween.TweenProperty(shaderMaterial, "shader_parameter/x_rot", 0.0f, RotationResetDuration);

            var yRotTween = CreatePropertyTween(SHADER_Y_ROT_PROPERTY)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            yRotTween.TweenProperty(shaderMaterial, "shader_parameter/y_rot", 0.0f, RotationResetDuration);

            _idleAnimationTime = 0;
            _requiresPerspectiveReset = true;
            xRotTween.Finished += () => _requiresPerspectiveReset = false;
        }
        
        SetupPerspectiveRectSize();
    }

    private void ResetScale()
    {
        AnimationUtils.AnimateScale(this, 1.0f, ScaleResetDuration, Tween.TransitionType.Elastic);
    }
    #endregion

    #region Tween Management
    private Tween CreatePropertyTween(string propertyKey)
    {
        // Kill existing tween for this property if it exists
        if (_propertyTweens.TryGetValue(propertyKey, out var existingTween))
        {
            if (existingTween != null && existingTween.IsValid())
            {
                existingTween.Kill();
            }
        }

        // Create new tween and store it
        var newTween = CreateTween();
        _propertyTweens[propertyKey] = newTween;

        // Set up cleanup when tween finishes
        newTween.Finished += () => {
            if (_propertyTweens.ContainsKey(propertyKey) && _propertyTweens[propertyKey] == newTween)
            {
                _propertyTweens.Remove(propertyKey);
            }
        };

        return newTween;
    }

    private void KillPropertyTween(string propertyKey)
    {
        if (_propertyTweens.TryGetValue(propertyKey, out var tween))
        {
            if (tween != null && tween.IsValid())
            {
                tween.Kill();
            }
            _propertyTweens.Remove(propertyKey);
        }
    }

    private void KillAllTweens()
    {
        foreach (var kvp in _propertyTweens.ToList())
        {
            if (kvp.Value != null && kvp.Value.IsValid())
            {
                kvp.Value.Kill();
            }
        }
        _propertyTweens.Clear();
    }
    #endregion
}