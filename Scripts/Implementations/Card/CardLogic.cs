using Godot;
using System;
using System.Linq;
using Scripts.Commands;
using Scripts.Commands.Card;
using Scripts.Config;

public partial class CardLogic : Button
{
	private IEventBus _eventBus;
	private IHoverManager _hoverManager;
	private ILogger _logger;
	private IGameCommandProcessor _commandProcessor;
	private bool _commandSystemReady = false;

	private Vector2 _distanceFromMouse;
	private Vector2 _initialMousePosition;
	private bool _mousePressed = false;
	private const float DRAG_THRESHOLD = GameConfig.DRAG_THRESHOLD;

	public bool IsSelected { get; private set; } = false;
	public bool IsHovering => _hoverManager != null ? _hoverManager.CurrentlyHoveringCard == Card : false;
	
	public bool IsDragging => _commandProcessor.CurrentState.Hand.DraggingCard?.CardId == Card.CardId;
	
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
		
		TrySetupCommandSystem();
	}

	private void TrySetupCommandSystem()
	{
		if (_commandSystemReady) return;
		
		_commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
		if (_commandProcessor != null)
		{
			_commandSystemReady = true;
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
			if (Card == null) return;
			if (!_commandSystemReady)
			{
				GD.Print("[CardLogic] Command system not ready, trying to set it up...");
				TrySetupCommandSystem();
				if (!_commandSystemReady) return;
			}
			
			if (!Visible || Card.Visual == null) return;
			
			bool needsPositionUpdate = IsDragging ||
				(Card.Visual.GetCenter() != GetTargetSlottedCenter());
			
			if (needsPositionUpdate)
			{
				UpdateVisualPosition((float)delta);
			}
			
			if (_mousePressed && !IsDragging)
			{
				CheckDragThreshold();
			}
			
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

	/// <summary>
	/// PURE COMMAND SYSTEM: Synchronize visual state with GameState
	/// </summary>
	private void SyncWithGameState()
	{
		if (_commandProcessor?.CurrentState == null || Card == null) return;
		
		try
		{
			var currentState = _commandProcessor.CurrentState;
			var cardState = currentState.Hand.Cards.FirstOrDefault(c => c.CardId == Card.CardId);

			if (cardState == null)
			{
				Card.QueueFree();
				return;
			}
			
			// Sync selection state
			if (IsSelected != cardState.IsSelected)
			{
				IsSelected = cardState.IsSelected;
				
				// DIRECT FIX: Immediately move both visual and interaction areas when selection changes
				ApplySelectionVisualFeedback();
			}
		}
		catch (Exception ex)
		{
			_logger?.LogError($"Error syncing with GameState: {ex.Message}", ex);
		}
	}

	/// <summary>
	/// DIRECT FIX: Apply selection visual feedback to both visual and interaction areas
	/// </summary>
	private void ApplySelectionVisualFeedback()
	{
		if (CardSlot == null || Card?.Visual == null) return;
		
		try
		{
			var basePosition = CardSlot.GetCenter();
			Vector2 targetPosition;
			
			if (IsSelected)
			{
				// Move up when selected
				targetPosition = basePosition + new Vector2(0, GameConfig.SELECTION_VERTICAL_OFFSET);
				GD.Print($"[CardLogic] DIRECT FIX: Moving card UP to {targetPosition} (selected)");
			}
			else
			{
				// Return to base position when deselected
				targetPosition = basePosition;
				GD.Print($"[CardLogic] DIRECT FIX: Moving card to base position {targetPosition} (deselected)");
			}
			
			// CRITICAL FIX: Move both visual AND interaction area together
			Card.Visual.SetCenter(targetPosition);
			this.SetCenter(targetPosition);
			
			GD.Print($"[CardLogic] DIRECT FIX: Both visual and interaction area moved to {targetPosition}");
		}
		catch (Exception ex)
		{
			GD.PrintErr($"[CardLogic] DIRECT FIX ERROR: {ex.Message}");
		}
	}

	private void CheckDragThreshold()
	{
		if (!_mousePressed || IsDragging) 
			return;
		
		// Check if any other card is dragging
		if (_commandSystemReady && _commandProcessor?.CurrentState?.Hand?.HasDraggingCard == true)
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
		_mousePressed = true;
		_initialMousePosition = GetGlobalMousePosition();
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

	/// <summary>
	/// PURE COMMAND SYSTEM: Execute selection commands
	/// </summary>
	private void HandleClick()
	{
		
		var isSelected = _commandProcessor.CurrentState.Hand.Cards
			.FirstOrDefault(card => card.CardId == Card.CardId)?.IsSelected ?? false;
		
		GameCommand command;
		if (isSelected)
		{
			command = new DeselectCardCommand(Card.CardId);
		}
		else
		{
			command = new SelectCardCommand(Card.CardId);
		}
		
		var success = _commandProcessor.ExecuteCommand(command);
		
		if (success)
		{
			// Force immediate sync to see the result
			SyncWithGameState();
		}
	}

	/// <summary>
	/// PURE COMMAND SYSTEM: Start dragging through StartDragCommand
	/// </summary>
	private void StartDragging()
	{
		if (!_commandSystemReady || _commandProcessor == null) return;
		
		var command = new StartDragCommand(Card.CardId);
		var success = _commandProcessor.ExecuteCommand(command);
		
		if (success)
		{
			_collisionShape?.SetDeferred("disabled", false);
			_eventBus?.Publish(new CardDragStartedEvent(Card));
		}
	}

	/// <summary>
	/// PURE COMMAND SYSTEM: End dragging through EndDragCommand
	/// </summary>
	private void StopDragging()
	{
		if (!_commandSystemReady || _commandProcessor == null) return;
		
		var command = new EndDragCommand(Card.CardId);
		var success = _commandProcessor.ExecuteCommand(command);
		
		if (success)
		{
			_collisionShape?.SetDeferred("disabled", true);
			this.SetCenter(GetTargetSlottedCenter());
			InvokePositionChanged();
			_eventBus?.Publish(new CardDragEndedEvent(Card));
		}
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
		var hasDraggingCard = _commandSystemReady && _commandProcessor?.CurrentState?.Hand?.HasDraggingCard == true;
		
		if (_hoverManager?.IsHoveringActive == true || hasDraggingCard) return;
		if (_hoverManager?.StartHover(Card) != true) return;

		_eventBus?.Publish(new CardHoverStartedEvent(Card));
	}

	public void OnMouseExited()
	{
		var hasDraggingCard = _commandSystemReady && _commandProcessor?.CurrentState?.Hand?.HasDraggingCard == true;
		
		if (_hoverManager?.CurrentlyHoveringCard != Card || hasDraggingCard) return;

		_hoverManager?.EndHover(Card);

		_eventBus?.Publish(new CardHoverEndedEvent(Card));
	}

	private void HandleMouseHover(InputEventMouseMotion mouseMotion)
	{
		var hasDraggingCard = _commandSystemReady && _commandProcessor?.CurrentState?.Hand?.HasDraggingCard == true;
		
		if (_hoverManager?.CurrentlyHoveringCard != Card || hasDraggingCard || IsDragging) return;

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
				var selectionOffset = Card.Visual.SelectionVerticalOffset;
				offset = new Vector2(0, selectionOffset);
			}
			catch (Exception ex)
			{
				_logger?.LogError($"Error accessing Card.Visual.SelectionVerticalOffset: {ex.Message}", ex);
			}
		}
		
		var baseCenter = CardSlot.GetCenter();
		var targetCenter = baseCenter + offset;
		return targetCenter;
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
