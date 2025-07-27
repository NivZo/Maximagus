using Godot;
using System;

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

	private bool _isSelected = false;
	private bool _isDragging = false;
    private Vector2 _lastPosition;
    private float _angularVelocity = 0f;
	private Vector2 _velocity; // Stores the current velocity of the card

	private Tween _tweenRot;
	private Tween _tweenHover;
	private Tween _tweenDestroy;
	private Tween _tweenHandle;

    private Control _textures;
    private TextureRect _cardTexture;
    private TextureRect _shadowTexture;

	public override void _Ready()
	{
		_textures = GetNode<Control>("Textures");
		_cardTexture = _textures.GetNode<TextureRect>("Card");
		_shadowTexture = _cardTexture.GetNode<TextureRect>("Shadow");

		Resized += SetupPerspectiveRectSize;
		SetupPerspectiveRectSize();

		// Convert angles to radians for calculations
		AngleXMax = Mathf.DegToRad(AngleXMax);
		AngleYMax = Mathf.DegToRad(AngleYMax);

		PivotOffset = Size / 2;
		_textures.PivotOffset = Size / 2;
	}

    public override void _Process(double delta)
    {
        UpdateShadow((float)delta);
        UpdatePosition((float)delta);
        UpdateSway((float)delta);
	}

	private void UpdateShadow(float delta)
	{
		Vector2 center = GetViewportRect().Size / 2.0f;
		float distance = GlobalPosition.X + Size.X / 2.0f - center.X;

		_shadowTexture.Position = new Vector2(
			Mathf.Lerp(0.0f, -Mathf.Sign(distance) * MaxOffsetShadow, Mathf.Abs(distance / center.X)),
			_shadowTexture.Position.Y
		);
		_shadowTexture.ZIndex = _isDragging ? 0 : -2;
	}

	public void OnClicked()
	{
		_isSelected = !_isSelected;
		// Scale = new(0.8f, 0.8f);
		Scale = Vector2.One;
		AnimateScale(HoverScale, HoverAnimationDuration, Tween.TransitionType.Elastic);
	}

	public void OnDragStarted()
	{
		ResetPerspective();
		AnimateScale(DragScale, HoverAnimationDuration, Tween.TransitionType.Elastic);

		_isDragging = true;
		_lastPosition = GlobalPosition;
		KillTween(ref _tweenHandle);
	}

	public void OnDragEnded()
	{
		_isDragging = false;
		OnHoverEnded();
	}

	public void OnHoverStarted()
	{
		AnimateScale(HoverScale, HoverAnimationDuration, Tween.TransitionType.Elastic);
	}

	public void OnHoverEnded()
	{
		if (_isDragging) return;
		ResetPerspective();
		ResetScale();
	}

	public void OnMouseMoved(Vector2 mousePosition)
	{
		Vector2 normalizedPosition = new Vector2(
			mousePosition.X / Size.X,
			mousePosition.Y / Size.Y
		);
		UpdateCardPerspective(normalizedPosition);
	}

    public void OnPositionChanged(float delta, Vector2 position, bool isDueToDragging)
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

        var shaderMaterial = _cardTexture.Material as ShaderMaterial;
        if (shaderMaterial != null)
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
        KillTween(ref _tweenHover);
        _tweenHover = CreateTween().SetEase(Tween.EaseType.Out).SetTrans(transitionType);
        _tweenHover.TweenMethod(Callable.From<float>(SetScale), Scale.X, targetScale, duration);
	}

	private void SetupPerspectiveRectSize()
	{
		var shaderMaterial = _cardTexture.Material as ShaderMaterial;
        if (shaderMaterial != null)
        {
            shaderMaterial.SetShaderParameter("rect_size", Size);
        }
	}

	private void ResetPerspective()
	{
		KillTween(ref _tweenRot);
		_tweenRot = CreateTween()
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Back)
			.SetParallel(true);

		var shaderMaterial = _cardTexture.Material as ShaderMaterial;
		if (shaderMaterial != null)
		{
			_tweenRot.TweenProperty(shaderMaterial, "shader_parameter/x_rot", 0.0f, RotationResetDuration);
			_tweenRot.TweenProperty(shaderMaterial, "shader_parameter/y_rot", 0.0f, RotationResetDuration);
		}
		SetupPerspectiveRectSize();
	}

	private void ResetRotationAfterDrag()
	{
		KillTween(ref _tweenHandle);
		_tweenHandle = CreateTween()
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Cubic);
		_tweenHandle.TweenProperty(this, "rotation", 0.0f, DragRotationResetDuration);
	}

	public void ResetScale()
	{
		AnimateScale(1.0f, ScaleResetDuration, Tween.TransitionType.Elastic);
	}

	public void StartDestroyAnimation()
	{
		_cardTexture.UseParentMaterial = true;
		
		KillTween(ref _tweenDestroy);
		_tweenDestroy = CreateTween()
			.SetEase(Tween.EaseType.InOut)
			.SetTrans(Tween.TransitionType.Cubic);

		var shaderMaterial = _cardTexture.Material as ShaderMaterial;
		if (shaderMaterial != null)
		{
			_tweenDestroy.TweenProperty(shaderMaterial, "shader_parameter/dissolve_value", 0.0f, DestroyDuration).From(1.0f);
		}
		
		_tweenDestroy.Parallel().TweenProperty(_shadowTexture, "self_modulate:a", 0.0f, ShadowFadeDuration);
	}
    
    private Vector2 GetPositionOffset()
    {
        return Size / 2 * (Vector2.One - Scale);
    }

	private void KillTween(ref Tween tween)
    {
        if (tween != null && tween.IsValid() && tween.IsRunning())
        {
            tween.Kill();
        }
        tween = null;
    }

	public override void _ExitTree()
	{
		KillTween(ref _tweenRot);
		KillTween(ref _tweenHover);
		KillTween(ref _tweenDestroy);
		KillTween(ref _tweenHandle);
	}
}
