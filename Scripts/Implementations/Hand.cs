using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Scripts.Commands;
using Scripts.Config;
using Scripts.State;
using Scripts.Utils;

public partial class Hand : Control
{
    [Export] float CardsCurveMultiplier = GameConfig.DEFAULT_CARDS_CURVE_MULTIPLIER;
    [Export] float CardsRotationMultiplier = GameConfig.DEFAULT_CARDS_ROTATION_MULTIPLIER;

    private IEventBus _eventBus;
    private ILogger _logger;
    private IGameCommandProcessor _commandProcessor;
    private OrderedContainer _cardSlotsContainer;
    private Node _cardsNode;
    private Node _cardSlotsNode;
    private HandLayoutCache _layoutCache;
    private HandState _lastHandState;

    private ImmutableArray<CardSlot> _cardSlots => _cardSlotsContainer
        ?.Where(n => n is CardSlot)
        .Cast<CardSlot>()
        .ToImmutableArray() ?? ImmutableArray<CardSlot>.Empty;

    private ImmutableArray<Card> _cards => _cardsNode
        .GetChildren()
        .OfType<Card>()
        .ToImmutableArray();
    
    private void OnHandStateChanged(IGameStateData oldState, IGameStateData newState)
    {
        try
        {
            var currentState = _commandProcessor.CurrentState;
            
            // Use more reliable comparison - check if the actual card lists differ
            bool handStateChanged = _lastHandState == null ||
                                   _lastHandState.Cards.Count != currentState.Hand.Cards.Count ||
                                   !CardsAreEqual(_lastHandState.Cards, currentState.Hand.Cards);
            
            if (handStateChanged)
            {
                _logger?.LogInfo($"[Hand] State changed - was {_lastHandState?.Cards.Count ?? 0} cards, now {currentState.Hand.Cards.Count} cards");
                _lastHandState = currentState.Hand;
                SyncVisualCardsWithState(currentState);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling hand state change", ex);
        }
    }
    
    // Helper method for comparing card state collections
    private bool CardsAreEqual(IEnumerable<CardState> cards1, IEnumerable<CardState> cards2)
    {
        if (cards1 == null && cards2 == null) return true;
        if (cards1 == null || cards2 == null) return false;
        
        var list1 = cards1.ToList();
        var list2 = cards2.ToList();
        
        if (list1.Count != list2.Count) return false;
        
        for (int i = 0; i < list1.Count; i++)
        {
            var card1 = list1[i];
            var card2 = list2[i];
            
            if (card1.CardId != card2.CardId ||
                card1.IsSelected != card2.IsSelected ||
                card1.IsDragging != card2.IsDragging ||
                card1.Position != card2.Position)
            {
                return false;
            }
        }
        
        return true;
    }
    
    // Synchronize visual cards with state
    private void SyncVisualCardsWithState(IGameStateData currentState)
    {
        var currentCards = _cards.ToArray();
        _logger?.LogInfo($"[Hand] Starting sync - Current visual cards: {currentCards.Length}, State cards: {currentState.Hand.Cards.Count}");
        
        // Log current visual card IDs
        var currentVisualCardIds = currentCards.Select(c => c.CardId).ToArray();
        _logger?.LogInfo($"[Hand] Current visual card IDs: [{string.Join(", ", currentVisualCardIds)}]");
        
        // Log state card IDs
        var stateCardIds = currentState.Hand.Cards.Select(c => c.CardId).ToArray();
        _logger?.LogInfo($"[Hand] State card IDs: [{string.Join(", ", stateCardIds)}]");

        var toRemove = currentCards.Where(card => !currentState.Hand.Cards.Any(c => c.CardId == card.CardId)).ToList();
        var toAdd = currentState.Hand.Cards.Where(c => !currentCards.Any(card => card.CardId == c.CardId)).ToList();

        _logger?.LogInfo($"[Hand] Cards to remove: {toRemove.Count}, Cards to add: {toAdd.Count}");

        foreach (var card in toRemove)
        {
            _logger?.LogInfo($"[Hand] Removing visual card {card.CardId} from hand");
            _cardSlotsContainer.RemoveElement(card.CardSlot);
            card.QueueFree();
        }

        foreach (var cardState in toAdd)
        {
            _logger?.LogInfo($"[Hand] Adding visual card {cardState.CardId} to hand");
            CreateVisualCardFromState(cardState);
        }
        
        var finalCards = _cards.ToArray();
        _logger?.LogInfo($"[Hand] Sync complete - Final visual cards: {finalCards.Length}");
    }
    
    private void CreateVisualCardFromState(CardState cardState)
    {
        try
        {
            _logger?.LogInfo($"[Hand] Creating visual card for state card {cardState.CardId}");
            
            // Create slot and card
            var slot = CardSlot.Create(_cardSlotsNode);
            var card = Card.Create(_cardsNode, slot, cardState.Resource, cardState.CardId);
            
            // Set the card's position
            card.GlobalPosition = GetViewportRect().Size + new Vector2(card.Size.X * 2, 0);
            
            // Now add the slot to the container (this triggers ElementsChanged)
            _cardSlotsContainer.InsertElement(slot);
            
            _logger?.LogInfo($"[Hand] Successfully created visual card for {cardState.Resource.CardName} with ID {cardState.CardId}");
        }
        catch (Exception ex)
        {
            _logger?.LogError($"[Hand] Error creating visual card: {ex.Message}", ex);
        }
    }

    public override void _Ready()
    {
        try
        {
            _eventBus = ServiceLocator.GetService<IEventBus>();
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
            _layoutCache = new HandLayoutCache();

            InitializeComponents();
            SubscribeToEvents();
            SetupEventHandlers();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error initializing Hand", ex);
            throw;
        }
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

    private void InitializeComponents()
    {
        _cardsNode = GetNode<Node>("Cards").ValidateNotNull("Cards");
        _cardSlotsNode = GetNode<Node>("CardSlots").ValidateNotNull("CardSlots");
        _cardSlotsContainer = GetNode<OrderedContainer>("CardSlotsContainer").ValidateNotNull("CardSlotsContainer");
    }

    private void SubscribeToEvents()
    {
        _eventBus?.Subscribe<CardHoverStartedEvent>(OnCardHoverStarted);
        _eventBus?.Subscribe<CardHoverEndedEvent>(OnCardHoverEnded);
    }

    private void SetupEventHandlers()
    {
        _cardSlotsContainer.ElementsChanged += OnElementsChanged;
        _commandProcessor.StateChanged += OnHandStateChanged;
    }

    private void AdjustFanEffect()
    {
        try
        {
            var count = _cardSlots.Length;
            
            if (count == 0) return; // No slots to adjust

            float baselineY = GlobalPosition.Y;
            var (positions, rotations) = _layoutCache.GetLayout(
                count,
                CardsCurveMultiplier,
                CardsRotationMultiplier,
                baselineY
            );

            for (int i = 0; i < count; i++)
            {
                var slot = _cardSlots[i];
                var card = _cards[i];

                // Skip if slot or card is null (timing issue during creation)
                if (slot == null || card == null)
                {
                    _logger?.LogWarning($"[Hand] Skipping null slot or card at index {i}");
                    continue;
                }

                // Handle Z-index ordering
                // card.ZIndex = i;
                MoveChildSafe(card, i);
                _logger.LogError("Moving child safe: " + card.Name + " to index: " + i);

                slot.TargetPosition = new Vector2(slot.TargetPosition.X, positions[i].Y);
                card.RotationDegrees = rotations[i];
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"[Hand] Error in AdjustFanEffect: {ex.Message}", ex);
        }
    }

    private void HandleDrag()
    {
        try
        {
            var draggingCard = _cards.FirstOrDefault(card => _commandProcessor.CurrentState.Hand.DraggingCard?.CardId == card.CardId);
            if (draggingCard == null) return;

            var draggedCardSlot = draggingCard.CardSlot;
            if (draggedCardSlot == null) return;

            var validSlots = _cardSlots.Where(slot =>
                slot.GetCenter().DistanceTo(draggingCard.GetCenter()) <= draggedCardSlot.MaxValidDistance);

            if (!validSlots.Any())
                return;

            var validTargetSlot = validSlots.MinBy(slot =>
                slot.GetCenter().DistanceSquaredTo(draggingCard.GetCenter()));

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
        // CallDeferred(MethodName.MoveChildSafe, @event.Card, _cardSlotsContainer.IndexOf(@event.Card.CardSlot));
    }

    private void OnCardHoverStarted(CardHoverStartedEvent @event)
    {
        // CallDeferred(MethodName.MoveChildSafe, @event.Card, _cardSlotsContainer.Count);
    }

    private void PerformSlotReorder(CardSlot draggedSlot, CardSlot targetSlot)
    {
        var draggedIndex = _cardSlots.IndexOf(draggedSlot);
        var targetIndex = _cardSlots.IndexOf(targetSlot);

        if (draggedIndex < 0 || targetIndex < 0 && draggedIndex != targetIndex)
            return;

        if (_cards[targetIndex] == null)
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
}
