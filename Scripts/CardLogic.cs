using Godot;
using System;

public partial class CardLogic : Button
{
	public event Action DragStarted;
	public event Action DragEnded;
	public event Action HoverStarted;
	public event Action HoverEnded;
	public event Action<Vector2> MouseMoved;
	public event Action<float, Vector2> PositionChanged;

	private static bool _isDraggingAnything = false;

	private Vector2 _distanceFromMouse;

	public bool IsDragging = false;
	public CardVisual Visual { get; private set; }
	public CardSlot CardSlot { get; private set; }

	private Area2D _interactionArea { get; set; }
	private CollisionShape2D _collisionShape { get; set; }
	

	public override void _Ready()
	{
		_interactionArea = GetNode<Area2D>("InteractionArea");
		_collisionShape = _interactionArea.GetNode<CollisionShape2D>("CollisionShape2D");
		_collisionShape.SetDeferred("disabled", true);

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
		}
	}

	public void SetCardSlot(CardSlot cardSlot)
	{
		CardSlot = cardSlot;
		GlobalPosition = CardSlot.GlobalPosition - Size / 2f;
		InvokePositionChanged();
	}

	private void FollowMouse(float delta)
	{
		Vector2 mousePos = GetGlobalMousePosition();
		GlobalPosition = mousePos - _distanceFromMouse;
		InvokePositionChanged(delta);
	}

	private void UpdateVisualPosition(float delta)
	{
		if (IsDragging)
		{
			FollowMouse(delta);
		}
		else if (Visual.GetCenter() != CardSlot.GetCenter())
		{
			InvokePositionChanged(delta);
		}
	}

	private void HandleMouseClick(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButtonEvent) return;
		if (mouseButtonEvent.ButtonIndex != MouseButton.Left) return;

		if (mouseButtonEvent.IsPressed() && !IsDragging)
		{
			_distanceFromMouse = GetGlobalMousePosition() - GlobalPosition;
			StartDragging();
		}
		else if (IsDragging)
		{
			StopDragging();
		}
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
			GlobalPosition = CardSlot.GlobalPosition - Size / 2f;
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
		HoverStarted?.Invoke();
	}

	public void OnMouseExited()
	{
		HoverEnded?.Invoke();
	}

	private void HandleMouseHover()
	{
		Vector2 mousePos = GetLocalMousePosition();
		MouseMoved?.Invoke(mousePos);
	}

	public void InvokePositionChanged(float? delta = null)
	{
		PositionChanged?.Invoke(delta ?? (float)GetProcessDeltaTime(), GlobalPosition);
	}

	public void DestroyCard()
	{
		Visual?.StartDestroyAnimation();
	}
}