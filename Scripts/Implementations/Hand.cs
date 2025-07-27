using Godot;
using System;
using System.Collections.Immutable;
using System.Linq;

public partial class Hand : Control
{
    private IEventBus _eventBus;
    private ILogger _logger;
    private OrderedContainer _cardSlotsContainer;
    private Node _cardsNode;
    private Node _cardSlotsNode;

    public ImmutableArray<CardSlot> CardSlots => _cardSlotsContainer
        ?.Where(n => n is CardSlot)
        .Cast<CardSlot>()
        .ToImmutableArray() ?? ImmutableArray<CardSlot>.Empty;
    
    public ImmutableArray<Card> Cards => CardSlots
        .Select(slot => slot.Card)
        .Where(card => card != null)
        .ToImmutableArray();

    public override void _Ready()
    {
        try
        {
            _eventBus = ServiceLocator.GetService<IEventBus>();
            _logger = ServiceLocator.GetService<ILogger>();
        
            InitializeComponents();
            SetupEventHandlers();
            InitializeCardSlots();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error initializing Hand", ex);
            throw;
        }
    }

    private void InitializeComponents()
    {
        _cardsNode = GetNode<Node>("Cards").ValidateNotNull("Cards");
        _cardSlotsNode = GetNode<Node>("CardSlots").ValidateNotNull("CardSlots");
        _cardSlotsContainer = GetNode<OrderedContainer>("CardSlotsContainer").ValidateNotNull("CardSlotsContainer");
    }

    private void SetupEventHandlers()
    {
        _cardSlotsContainer.ElementsChanged += OnElementsChanged;
    }

    public override void _Process(double delta)
    {
        try
        {
            base._Process(delta);
            HandleDrag();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error in Hand process", ex);
        }
    }

    public override void _Input(InputEvent @event)
    {
        try
        {
            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                HandleKeyInput(keyEvent);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling input in Hand", ex);
        }
    }

    private void HandleKeyInput(InputEventKey keyEvent)
    {
        switch (keyEvent.Keycode)
        {
            case Key.Left:
                if (_cardSlotsContainer.Count > 0)
                    _cardSlotsContainer.RemoveElementAt(_cardSlotsContainer.Count - 1);
                break;
                
            case Key.Right:
                _cardSlotsContainer.InsertElement(_cardSlotsContainer.Count, CardSlot.Create(_cardSlotsNode));
                break;
        }
    }

    public void InitializeCardSlots()
    {
        try
        {
            var slots = _cardSlotsNode.GetChildren()
                .OfType<CardSlot>()
                .OrderBy(slot => slot.GlobalPosition.X)
                .ToList();

            for (int i = 0; i < slots.Count; i++)
            {
                _cardSlotsContainer[i] = slots[i];
            }

            _eventBus?.Publish(new HandCardSlotsChangedEvent(this));
            
            // Create cards for each slot
            foreach (var slot in CardSlots)
            {
                Card.Create(_cardsNode, slot);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error initializing card slots", ex);
            throw;
        }
    }

    private void HandleDrag()
    {
        try
        {
            var draggedCardSlot = CardSlots.FirstOrDefault(slot => slot.Card?.IsDragging ?? false);
            if (draggedCardSlot?.Card == null)
                return;

            var validSlots = CardSlots.Where(slot => 
                slot.GetCenter().DistanceTo(draggedCardSlot.Card.CardLogic.GetCenter()) <= draggedCardSlot.MaxValidDistance);

            if (!validSlots.Any())
                return;

            var validTargetSlot = validSlots.MinBy(slot => 
                slot.GetCenter().DistanceSquaredTo(draggedCardSlot.Card.CardLogic.GetCenter()));

            if (validTargetSlot != null && validTargetSlot != draggedCardSlot)
            {
                PerformSlotReorder(draggedCardSlot, validTargetSlot);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling drag", ex);
        }
    }

    private void PerformSlotReorder(CardSlot draggedSlot, CardSlot targetSlot)
    {
        var draggedIndex = CardSlots.IndexOf(draggedSlot);
        var targetIndex = CardSlots.IndexOf(targetSlot);
        
        if (draggedIndex < 0 || targetIndex < 0 && draggedIndex != targetIndex)
            return;

        if (targetSlot.Card == null)
        {
            _cardSlotsContainer.SwapElements(draggedIndex, targetIndex);
            GD.Print($"Swapping dragged: {draggedIndex} and target: {targetIndex}");
        }
        else
        {
            _cardSlotsContainer.MoveElement(draggedIndex, targetIndex);
            GD.Print($"Moving dragged: {draggedIndex} and target: {targetIndex}");
        }
    }

    private void OnElementsChanged()
    {
        try
        {
            var cards = Cards.ToList();
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].ZIndex = i;
                _cardsNode.MoveChild(cards[i], i);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling elements changed", ex);
        }
    }
}