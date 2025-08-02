using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CardVisual : Control
{
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
    [Export(PropertyHint.Range, "0, 100, 1")] public float MaxSpeed = 1f;
    
    [ExportGroup("Sway Physics")]
    [Export(PropertyHint.Range, "0, 500, 1")] public float Stiffness { get; set; } = 150f;
    [Export(PropertyHint.Range, "0, 50, 0.1")] public float Damping { get; set; } = 10f;
    [Export(PropertyHint.Range, "0, 0.01, 0.0001")] public float VelocityFactor { get; set; } = 0.0015f;
    [Export(PropertyHint.Range, "0, 90, 1")] public float MaxRotationDegrees { get; set; } = 15f;

    private ILogger _logger;
    private IEventBus _eventBus;
    private Card _parentCard;
    private Tooltip _tooltip;
    private bool _isSelected => _parentCard?.IsSelected ?? false;
    private bool _isDragging => _parentCard?.IsDragging ?? false;
    private bool _isHovering => _parentCard?.IsHovering ?? false;
    private bool _requiresPerspectiveReset = false;
    private float _idleAnimationTime = 0;
    private float _angularVelocity = 0f;
    private Vector2 _lastPosition;
    private Vector2 _velocity;
    private Vector2 _shadowBasePosition;

    private readonly Dictionary<string, Tween> _propertyTweens = new();
    private Control _textures;
    private TextureRect _cardTexture;
    private TextureRect _artTexture;
    private TextureRect _shadowTexture;

    // Define tween property keys as constants for consistency
    private const string ROTATION_PROPERTY = "rotation";
    private const string SHADER_X_ROT_PROPERTY = "shader_x_rot";
    private const string SHADER_Y_ROT_PROPERTY = "shader_y_rot";

    public string Title;


    public override void _Ready()
    {
        try
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _eventBus = ServiceLocator.GetService<IEventBus>();
            _parentCard = GetParent<Card>();

            InitializeComponents();
            SetupPerspective();
            SetupPivots();
            SubscribeToEvents();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error initializing CardVisual", ex);
            throw;
        }
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

    private void SetupPerspective()
    {
        // Convert angles to radians for calculations
        _idleAnimationTime += (float)new Random().NextDouble() * 20 - 10;
        AngleXMax = Mathf.DegToRad(AngleXMax);
        AngleYMax = Mathf.DegToRad(AngleYMax);
    }

    private void SetupPivots()
    {
        PivotOffset = Size / 2;
        _textures.PivotOffset = Size / 2;
    }

    private void SubscribeToEvents()
    {
        _eventBus?.Subscribe<CardDragStartedEvent>(OnCardDragStarted);
        _eventBus?.Subscribe<CardDragEndedEvent>(OnCardDragEnded);
        _eventBus?.Subscribe<CardHoverStartedEvent>(OnCardHoverStarted);
        _eventBus?.Subscribe<CardHoverEndedEvent>(OnCardHoverEnded);
        _eventBus?.Subscribe<CardClickedEvent>(OnCardClicked);
        _eventBus?.Subscribe<CardPositionChangedEvent>(OnCardPositionChanged);
        _eventBus?.Subscribe<CardMouseMovedEvent>(OnCardMouseMoved);
    }

    private void UnsubscribeFromEvents()
    {
        _eventBus?.Unsubscribe<CardDragStartedEvent>(OnCardDragStarted);
        _eventBus?.Unsubscribe<CardDragEndedEvent>(OnCardDragEnded);
        _eventBus?.Unsubscribe<CardHoverStartedEvent>(OnCardHoverStarted);
        _eventBus?.Unsubscribe<CardHoverEndedEvent>(OnCardHoverEnded);
        _eventBus?.Unsubscribe<CardClickedEvent>(OnCardClicked);
        _eventBus?.Unsubscribe<CardPositionChangedEvent>(OnCardPositionChanged);
        _eventBus?.Subscribe<CardMouseMovedEvent>(OnCardMouseMoved);
    }

    public override void _Process(double delta)
    {
        try
        {
            UpdateShadow((float)delta);
            UpdatePosition((float)delta);
            UpdateSway((float)delta);
            UpdateCardPerspectiveIdle((float)delta);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error in CardVisual process", ex);
        }
    }

    public void SetArt(Texture2D texture2D)
    {
        _artTexture.Texture = texture2D;
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

    // Event handlers - filtering for this card only
    private void OnCardDragStarted(CardDragStartedEvent evt)
    {
        if (evt.Card != _parentCard) return;
        OnDragStarted();
    }

    private void OnCardDragEnded(CardDragEndedEvent evt)
    {
        if (evt.Card != _parentCard) return;
        OnDragEnded();
    }

    private void OnCardHoverStarted(CardHoverStartedEvent evt)
    {
        if (evt.Card != _parentCard) return;
        OnHoverStarted();
    }

    private void OnCardHoverEnded(CardHoverEndedEvent evt)
    {
        if (evt.Card != _parentCard) return;
        OnHoverEnded();
    }

    private void OnCardClicked(CardClickedEvent evt)
    {
        if (evt.Card != _parentCard) return;
        OnClicked();
    }

    private void OnCardPositionChanged(CardPositionChangedEvent evt)
    {
        if (evt.Card != _parentCard) return;
        OnPositionChanged(evt.Delta, evt.Position, evt.IsDueToDragging);
    }

    private void OnCardMouseMoved(CardMouseMovedEvent evt)
    {
        if (evt.Card != _parentCard) return;
        OnMouseMoved(evt.LocalPosition);
    }

    private void OnClicked()
    {
        Scale = Vector2.One;
        AnimationUtils.AnimateScale(this, HoverScale, ClickAnimationDuration, Tween.TransitionType.Elastic);
    }

    private void OnDragStarted()
    {
        KillAllTweens();
        ResetPerspective();
        
        AnimationUtils.AnimateScale(this, DragScale, HoverAnimationDuration, Tween.TransitionType.Elastic);

        _lastPosition = GlobalPosition;

        _tooltip?.HideTooltip();
    }

    private void OnDragEnded()
    {
        OnHoverEnded();
    }

    private void OnHoverStarted()
    {
        ZIndex = LayerIndices.HoveredCard;
        AnimationUtils.AnimateScale(this, HoverScale, HoverAnimationDuration, Tween.TransitionType.Elastic);

        if (_tooltip == null)
        {
            _tooltip = Tooltip.Create(_parentCard.Logic, new(0, -Size.Y), Title, "");
        }

        _tooltip.ShowTooltip();
    }

    private void OnHoverEnded()
    {
        if (_isDragging) return;

        ZIndex = LayerIndices.Base;
        ResetPerspective();
        ResetScale();

        _tooltip?.HideTooltip();
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

    private void OnPositionChanged(float delta, Vector2 position, bool isDueToDragging)
    {
        if (!isDueToDragging && _isDragging) return;
        if (delta > 0)
        {
            _velocity = (position - _lastPosition) / delta;
        }
        _lastPosition = position;
    }

    private void UpdatePosition(float delta)
    {
        var center = this.GetCenter();
        var raw = center.Lerp(_lastPosition + GetPositionOffset(), delta * 10f);
        var clamped = raw.Clamp(center - Size / 2, center + Size / 2);
        this.SetCenter(clamped);
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

    private void UpdateCardPerspectiveIdle(float delta)
    {
        if (_isHovering && !_isDragging) return;
        if (_requiresPerspectiveReset) return;
        
        else
        {
            float sine = Mathf.Sin(_idleAnimationTime) ;
            float cosine = Mathf.Cos(_idleAnimationTime) - 1;
            float rotX = Mathf.RadToDeg(Mathf.LerpAngle(0, AngleXMax * AngleIdleRatio, sine)) - AngleXMax * AngleIdleRatio * .5f;
            float rotY = Mathf.RadToDeg(Mathf.LerpAngle(0, -AngleYMax * AngleIdleRatio, cosine)) + AngleYMax * AngleIdleRatio * .5f;

            var shader = _cardTexture.Material as ShaderMaterial;
            shader.SetShaderParameter("x_rot", rotY);
            shader.SetShaderParameter("y_rot", rotX);
            _idleAnimationTime += delta;
        }
    }


    private void SetScale(float targetScale)
    {
        Scale = new Vector2(targetScale, targetScale);
    }

    private void SetupPerspectiveRectSize()
    {
        if (_cardTexture?.Material is ShaderMaterial shaderMaterial)
        {
            shaderMaterial.SetShaderParameter("rect_size", Size);
        }
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

    private void ResetRotationAfterDrag()
    {
        var tween = CreatePropertyTween(ROTATION_PROPERTY)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Cubic);
        
        tween.TweenProperty(this, "rotation", 0.0f, DragRotationResetDuration);
    }

    private void ResetScale()
    {
        AnimationUtils.AnimateScale(this, 1.0f, ScaleResetDuration, Tween.TransitionType.Elastic);
    }

    private Vector2 GetPositionOffset()
    {
        return Size / 2 * (Vector2.One - Scale);
    }

    /// <summary>
    /// Creates or replaces a tween for a specific property. Only one tween per property can exist at a time.
    /// </summary>
    /// <param name="propertyKey">Unique identifier for the property being tweened</param>
    /// <returns>The new tween instance</returns>
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

    /// <summary>
    /// Kills a specific property tween if it exists
    /// </summary>
    /// <param name="propertyKey">The property key to kill</param>
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

    public override void _ExitTree()
    {
        try
        {
            UnsubscribeFromEvents();
            KillAllTweens();
            base._ExitTree();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error during CardVisual cleanup", ex);
        }
    }
}