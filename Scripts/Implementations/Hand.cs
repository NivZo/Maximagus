using Godot;
using System;
using System.Collections.Immutable;
using System.Linq;

public partial class Hand : Control
{
    public static Hand Instance { get; private set; }

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

    public ImmutableArray<Card> SelectedCards => Cards
        .Where(card => card.IsSelected)
        .ToImmutableArray();

    public override void _Ready()
    {
        try
        {
            Instance = this;
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
            var rnd = new Random();
            foreach (var slot in CardSlots)
            {
                var resource = new CardResource()
                {
                    Value = rnd.Next(10),
                };
                Card.Create(_cardsNode, slot, resource);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error initializing card slots", ex);
            throw;
        }
    }

    public void Discard(Card card) => Discard([card]);

    public void Discard(Card[] cards)
    {
        foreach (var card in cards)
        {
            if (Cards.Contains(card))
            {
                GD.Print($"Removed card {card.Resource.Value} from slot {_cardSlotsContainer.IndexOf(card.Logic.CardSlot)+1}");
                _cardSlotsContainer.RemoveElement(card.Logic.CardSlot);
                card.Logic.CardSlot.QueueFree();
                card.QueueFree();
            }
        }
    }

    public void DrawAndAppend(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            var slot = CardSlot.Create(_cardSlotsNode);
            var card = Card.Create(_cardsNode, slot, new() { Value = 1 });
            card.GlobalPosition = GetViewportRect().Size + new Vector2(card.Size.X * 2, 0);
            _cardSlotsContainer.InsertElement(slot);

            GD.Print($"Added card {card.Resource.Value} to slot {_cardSlotsContainer.IndexOf(slot)+1}");
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
                slot.GetCenter().DistanceTo(draggedCardSlot.Card.Logic.GetCenter()) <= draggedCardSlot.MaxValidDistance);

            if (!validSlots.Any())
                return;

            var validTargetSlot = validSlots.MinBy(slot =>
                slot.GetCenter().DistanceSquaredTo(draggedCardSlot.Card.Logic.GetCenter()));

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