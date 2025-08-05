using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Maximagus.Scripts.Spells.Abstractions;
using Maximagus.Scripts.Events;
using Scripts.State;
using Scripts.Commands;

public partial class Hand : Control, IGameStateObserver
{
    public static Hand Instance { get; private set; }

    [Export] float CardsCurveMultiplier = 20;
    [Export] float CardsRotationMultiplier = 5;

    private IEventBus _eventBus;
    private ILogger _logger;
    private Deck _deck;
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
            _deck = new();

            SubscribeToEvents();
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

    private void SubscribeToEvents()
    {
        _eventBus?.Subscribe<CardHoverStartedEvent>(OnCardHoverStarted);
        _eventBus?.Subscribe<CardHoverEndedEvent>(OnCardHoverEnded);
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

            _eventBus?.Publish(new HandCardSlotsChangedEvent());

            // Create cards for each slot
            var rnd = new Random();
            foreach (var slot in CardSlots)
            {
                var resource = _deck.GetNext();
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

    public void Discard(IEnumerable<Card> cards)
    {
        foreach (var card in cards)
        {
            if (Cards.Contains(card))
            {
                GD.Print($"Removed card {card.Resource.CardName} from slot {_cardSlotsContainer.IndexOf(card.Logic.CardSlot)+1}");
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
            var resource = _deck.GetNext();
            var slot = CardSlot.Create(_cardSlotsNode);
            var card = Card.Create(_cardsNode, slot, resource);
            card.GlobalPosition = GetViewportRect().Size + new Vector2(card.Size.X * 2, 0);
            _cardSlotsContainer.InsertElement(slot);

            GD.Print($"Added card {card.Resource.CardName} to slot {_cardSlotsContainer.IndexOf(slot)+1}");
        }
    }

    private void AdjustFanEffect()
    {
        float baselineY = GlobalPosition.Y;

        var count = _cardSlotsContainer.Count;
        for (int i = 0; i < count; i++)
        {
            var slot = CardSlots[i];
            var card = slot.Card;

            // Handle Z
            card.ZIndex = i;
            _cardsNode.MoveChild(card, i);

            // Handle curve
            float normalizedPos = (count > 1) ? (2.0f * i / count - 1.0f) : 0;
            float yOffset = Mathf.Pow(normalizedPos, 2) * -CardsCurveMultiplier;
            slot.TargetPosition = new Vector2(slot.TargetPosition.X, baselineY - yOffset);


            // Handle rotation
            float rotation = normalizedPos * CardsRotationMultiplier;
            card.Visual.RotationDegrees = rotation;
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

    private void OnCardHoverEnded(CardHoverEndedEvent @event)
    {
        CallDeferred(MethodName.MoveChildSafe, @event.Card, _cardSlotsContainer.IndexOf(@event.Card.Logic.CardSlot));
    }

    private void OnCardHoverStarted(CardHoverStartedEvent @event)
    {
        CallDeferred(MethodName.MoveChildSafe, @event.Card, _cardSlotsContainer.Count);
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

    private void MoveChildSafe(Card card, int index)
    {
        if (card != null && !card.IsQueuedForDeletion())
        {
            _cardsNode.MoveChild(card, index);
        }
    }

    private void OnElementsChanged()
    {
        try
        {
            AdjustFanEffect();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling elements changed", ex);
        }
    }
    /// <summary>
    /// Called when the GameState changes - updates the Hand to match the new state
    /// </summary>
    public void OnGameStateChanged(IGameStateData previousState, IGameStateData newState)
    {
        Console.WriteLine("[Hand] GameState changed - syncing Hand with new state");
        
        try
        {
            // Sync card selections
            SyncCardSelections(newState.Hand);
            // Sync card count (now enabled since GameState is properly initialized)
            SyncCardCount(newState.Hand);
            
            
            Console.WriteLine($"[Hand] Synced with GameState: {newState.Hand.Count} cards, {newState.Hand.SelectedCount} selected");
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error syncing Hand with GameState", ex);
        }
    }

    /// <summary>
    /// Sets the GameCommandProcessor and subscribes to state changes
    /// </summary>
    public void SetGameCommandProcessor(GameCommandProcessor commandProcessor)
    {
        if (commandProcessor != null)
        {
            commandProcessor.StateChanged += OnGameStateChanged;
            Console.WriteLine("[Hand] Subscribed to GameState changes");
        }
    }

    /// <summary>
    /// Syncs the visual card selections with the GameState
    /// </summary>
    private void SyncCardSelections(HandState handState)
    {
        var gameStateSelectedIds = handState.SelectedCardIds;
        var realCards = Cards;

        foreach (var card in realCards)
        {
            var cardId = card.GetInstanceId().ToString();
            var shouldBeSelected = gameStateSelectedIds.Contains(cardId);
            var currentlySelected = card.IsSelected;

            if (shouldBeSelected != currentlySelected)
            {
                // TODO: Need to find proper way to sync card selection with GameState
                // For now, just log the difference
                Console.WriteLine($"[Hand] Card {cardId} selection mismatch - GameState: {shouldBeSelected}, UI: {currentlySelected}");
            }
        }
    }

    /// <summary>
    /// Syncs the card count with the GameState (adds/removes cards as needed)
    /// </summary>
    private void SyncCardCount(HandState handState)
    {
        var currentCardCount = Cards.Length;
        var targetCardCount = handState.Count;

        if (currentCardCount < targetCardCount)
        {
            // Add missing cards
            var cardsToAdd = targetCardCount - currentCardCount;
            DrawAndAppend(cardsToAdd);
            Console.WriteLine($"[Hand] Added {cardsToAdd} cards to match GameState");
        }
        else if (currentCardCount > targetCardCount)
        {
            // Remove excess cards
            var cardsToRemove = currentCardCount - targetCardCount;
            var cardsToDiscard = Cards.Take(cardsToRemove).ToList();
            Discard(cardsToDiscard);
            Console.WriteLine($"[Hand] Removed {cardsToRemove} cards to match GameState");
        }
    }
}
