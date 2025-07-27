using Godot;
using System;

public partial class CardLogic : Button
{
	public event Action DragStarted;
	public event Action DragEnded;
	public event Action HoverStarted;
	public event Action HoverEnded;
	public event Action<Vector2> MouseMoved;
	public event Action<float, Vector2, bool> PositionChanged;
	public event Action Clicked; // New click event

	private static bool _isDraggingAnything = false;

	private Vector2 _distanceFromMouse;
	private Vector2 _initialMousePosition;
	private bool _mousePressed = false;
	private const float DRAG_THRESHOLD = 5.0f; // Minimum distance to start dragging

	public bool IsSelected { get; private set; } = false;
	public bool IsHovering { get; private set; } = false;
	public bool IsDragging { get; private set; } = false;
	public CardVisual Visual { get; private set; }
	public CardSlot CardSlot { get; private set; }

	private Area2D _interactionArea { get; set; }
	private CollisionShape2D _collisionShape { get; set; }

	public override void _Ready()
	{
		_interactionArea = GetNode<Area2D>("InteractionArea");
		_collisionShape = _interactionArea.GetNode<CollisionShape2D>("CollisionShape2D");
		_collisionShape.SetDeferred("disabled", true);
		(_collisionShape.Shape as RectangleShape2D).Size = Size;
		_interactionArea.Position = Size / 2f;

		GuiInput += OnGuiInput;
		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;

		Hand.Instance.CardSlotsChanged += () =>
		{
			InvokePositionChanged();
		};
	}

	public override void _Process(double delta)
	{
		UpdateVisualPosition((float)delta);
		
		// Check if we should start dragging while mouse is pressed
		if (_mousePressed && !IsDragging && !_isDraggingAnything)
		{
			Vector2 currentMousePos = GetGlobalMousePosition();
			float distance = _initialMousePosition.DistanceTo(currentMousePos);
			
			if (distance > DRAG_THRESHOLD)
			{
				_distanceFromMouse = currentMousePos - this.GetCenter();
				StartDragging();
			}
		}
	}

	public void SetVisual(CardVisual visual)
	{
		if (Visual != null)
		{
			// Disconnect previous visual events
			DragStarted -= Visual.OnDragStarted;
			DragEnded -= Visual.OnDragEnded;
			HoverStarted -= Visual.OnHoverStarted;
			HoverEnded -= Visual.OnHoverEnded;
			MouseMoved -= Visual.OnMouseMoved;
			PositionChanged -= Visual.OnPositionChanged;
			Clicked -= Visual.OnClicked;
		}

		Visual = visual;

		if (Visual != null)
		{
			// Connect new visual events
			DragStarted += Visual.OnDragStarted;
			DragEnded += Visual.OnDragEnded;
			HoverStarted += Visual.OnHoverStarted;
			HoverEnded += Visual.OnHoverEnded;
			MouseMoved += Visual.OnMouseMoved;
			PositionChanged += Visual.OnPositionChanged;
			Clicked += Visual.OnClicked;
		}
	}

	public void SetCardSlot(CardSlot cardSlot)
	{
		CardSlot = cardSlot;
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
		else if (Visual.GetCenter() != GetTargetSlottedCenter())
		{
			InvokePositionChanged(delta);
		}
	}

	private void HandleMouseClick(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButtonEvent) return;
		if (mouseButtonEvent.ButtonIndex != MouseButton.Left) return;

		if (mouseButtonEvent.IsPressed())
		{
			// Mouse button pressed
			if (!_isDraggingAnything)
			{
				_mousePressed = true;
				_initialMousePosition = GetGlobalMousePosition();
			}
		}
		else
		{
			// Mouse button released
			if (IsDragging)
			{
				StopDragging();
			}
			else if (_mousePressed)
			{
				// This was a click (no drag occurred)
				HandleClick();
			}
			
			_mousePressed = false;
		}
	}

	private void HandleClick()
	{
		IsSelected = !IsSelected;
		this.SetCenter(GetTargetSlottedCenter());
		
		InvokePositionChanged();
		Clicked?.Invoke();
	}

	private void StartDragging()
	{
		if (_isDraggingAnything) return;
		_isDraggingAnything = true;
		IsDragging = true;
		_collisionShape.SetDeferred("disabled", false);
		
		DragStarted?.Invoke();
	}

	private void StopDragging()
	{
		_isDraggingAnything = false;
		IsDragging = false;
		_collisionShape.SetDeferred("disabled", true);

		if (true)
		{
			this.SetCenter(GetTargetSlottedCenter());
			InvokePositionChanged();
		}

		DragEnded?.Invoke();
	}

	public void OnGuiInput(InputEvent @event)
	{
		HandleMouseClick(@event);

		if (IsDragging) return;
		if (@event is not InputEventMouseMotion) return;
		
		HandleMouseHover();
	}

	public void OnMouseEntered()
	{
		if (_isDraggingAnything) return;
		if (IsDragging) return;

		IsHovering = true;
		HoverStarted?.Invoke();
	}

	public void OnMouseExited()
	{
		IsHovering = false;
		HoverEnded?.Invoke();
	}

	private void HandleMouseHover()
	{
		Vector2 mousePos = GetLocalMousePosition();
		MouseMoved?.Invoke(mousePos);
	}

	private Vector2 GetTargetCenter()
	{
		return IsDragging ? GetTargetFollowMouseCenter() : GetTargetSlottedCenter();
	}

	private Vector2 GetTargetSlottedCenter()
	{
		return CardSlot.GetCenter() + (IsSelected ? new Vector2(0, Visual.SelectionVerticalOffset) : Vector2.Zero);
	}

	private Vector2 GetTargetFollowMouseCenter()
	{
		return this.GetCenter();
	}

	public void InvokePositionChanged(float? delta = null, bool isDueToDragging = false)
	{
		PositionChanged?.Invoke(delta ?? (float)GetProcessDeltaTime(), GetTargetCenter(), isDueToDragging);
	}

	public void DestroyCard()
	{
		Visual?.StartDestroyAnimation();
	}
}