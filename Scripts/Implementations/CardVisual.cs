using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CardVisual : Control
{
    [ExportGroup("Visual Settings")]
    [Export] public float AngleXMax = 15.0f;
    [Export] public float AngleYMax = 15.0f;
    [Export] public float MaxOffsetShadow = 50.0f;
    [Export] public float SelectionVerticalOffset = -64.0f;

    [ExportGroup("Animation Settings")]
    [Export] public float HoverScale = 1.1f;
    [Export] public float DragScale = 1.2f;
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
    private bool _isSelected = false;
    private bool _isDragging = false;
    private Vector2 _lastPosition;
    private float _angularVelocity = 0f;
    private Vector2 _velocity;

    // Property-based tween management
    private readonly Dictionary<string, Tween> _propertyTweens = new();
    private Control _textures;
    private TextureRect _cardTexture;
    private TextureRect _shadowTexture;

    // Define tween property keys as constants for consistency
    private const string SCALE_PROPERTY = "scale";
    private const string ROTATION_PROPERTY = "rotation";
    private const string SHADER_X_ROT_PROPERTY = "shader_x_rot";
    private const string SHADER_Y_ROT_PROPERTY = "shader_y_rot";
    private const string DISSOLVE_PROPERTY = "dissolve";
    private const string SHADOW_ALPHA_PROPERTY = "shadow_alpha";

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
        _shadowTexture = _cardTexture.GetNode<TextureRect>("Shadow").ValidateNotNull("Shadow");

        Resized += SetupPerspectiveRectSize;
        SetupPerspectiveRectSize();
    }

    private void SetupPerspective()
    {
        // Convert angles to radians for calculations
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
        _eventBus?.Subscribe<CardDestroyStartedEvent>(OnCardDestroyStarted);
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
        _eventBus?.Unsubscribe<CardDestroyStartedEvent>(OnCardDestroyStarted);
    }

    public override void _Process(double delta)
    {
        try
        {
            UpdateShadow((float)delta);
            UpdatePosition((float)delta);
            UpdateSway((float)delta);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Error in CardVisual process", ex);
        }
    }

    private void UpdateShadow(float delta)
    {
        Vector2 screenCenter = GetViewportRect().GetCenter();
        float distance = this.GetCenter().X - screenCenter.X;

        _shadowTexture.Position = new Vector2(
            Mathf.Lerp(0.0f, -Mathf.Sign(distance) * MaxOffsetShadow, Mathf.Abs(distance / screenCenter.X)),
            _shadowTexture.Position.Y
        );
        _shadowTexture.ZIndex = _isDragging ? 0 : -2;
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

    private void OnCardDestroyStarted(CardDestroyStartedEvent evt)
    {
        if (evt.Card != _parentCard) return;
        StartDestroyAnimation();
    }

    // Original visual methods - now private
    private void OnClicked()
    {
        _isSelected = !_isSelected;
        Scale = Vector2.One;
        AnimateScale(HoverScale, ClickAnimationDuration, Tween.TransitionType.Elastic);
    }

    private void OnDragStarted()
    {
        KillAllTweens();
        ResetPerspective();
        OnHoverStarted();

        _isDragging = true;
        _lastPosition = GlobalPosition;
    }

    private void OnDragEnded()
    {
        _isDragging = false;
        OnHoverEnded();
    }

    private void OnHoverStarted()
    {
        AnimateScale(HoverScale, HoverAnimationDuration, Tween.TransitionType.Elastic);
    }

    private void OnHoverEnded()
    {
        if (_isDragging) return;
        ResetPerspective();
        ResetScale();
    }

    private void OnMouseMoved(Vector2 mousePosition)
    {
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

    private void SetScale(float targetScale)
    {
        Scale = new Vector2(targetScale, targetScale);
    }

    private void AnimateScale(float targetScale, float duration, Tween.TransitionType transitionType)
    {
        var tween = CreatePropertyTween(SCALE_PROPERTY)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(transitionType);
        
        tween.TweenMethod(Callable.From<float>(SetScale), Scale.X, targetScale, duration);
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
        AnimateScale(1.0f, ScaleResetDuration, Tween.TransitionType.Elastic);
    }

    private void StartDestroyAnimation()
    {
        try
        {
            _cardTexture.UseParentMaterial = true;
            
            if (_cardTexture?.Material is ShaderMaterial shaderMaterial)
            {
                var dissolveTween = CreatePropertyTween(DISSOLVE_PROPERTY)
                    .SetEase(Tween.EaseType.InOut)
                    .SetTrans(Tween.TransitionType.Cubic);
                
                dissolveTween.TweenProperty(shaderMaterial, "shader_parameter/dissolve_value", 0.0f, DestroyDuration).From(1.0f);
            }
            
            var shadowTween = CreatePropertyTween(SHADOW_ALPHA_PROPERTY)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Cubic);
            
            shadowTween.TweenProperty(_shadowTexture, "self_modulate:a", 0.0f, ShadowFadeDuration);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error starting destroy animation", ex);
        }
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