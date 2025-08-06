using Godot;
using System;
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

    private ImmutableArray<Card> _cards => _cardSlots
        .Select(slot => slot.Card)
        .Where(card => card != null)
        .ToImmutableArray();
    
    private void OnHandStateChanged(IGameStateData oldState, IGameStateData newState)
    {
        try
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState.Hand.GetHashCode() != _lastHandState?.GetHashCode())
            {
                _logger?.LogInfo($"[Hand] State changed: {currentState.GetHashCode()}");
                _lastHandState = currentState.Hand;
                SyncVisualCardsWithState(currentState);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling hand state change", ex);
        }
    }
    
    // Synchronize visual cards with state
    private void SyncVisualCardsWithState(IGameStateData currentState)
    {
        var toRemove = _cards.Where(card => !currentState.Hand.Cards.Any(c => c.CardId == card.CardId)).ToList();
        var toAdd = currentState.Hand.Cards.Where(c => !_cards.Any(card => card.CardId == c.CardId)).ToList();

        foreach (var card in toRemove)
        {
            _logger?.LogInfo($"[Hand] Removing visual card {card.CardId} from hand");
            _cardSlotsContainer.RemoveElement(card.Logic.CardSlot);
            card.QueueFree();
        }

        foreach (var cardState in toAdd)
        {
            _logger?.LogInfo($"[Hand] Adding visual card {cardState.CardId} to hand");
            CreateVisualCardFromState(cardState);
        }
    }
    
    private void CreateVisualCardFromState(CardState cardState)
    {
        try
        {
            _logger?.LogInfo($"[Hand] Creating visual card for state card {cardState.CardId}");
            var slot = CardSlot.Create(_cardSlotsNode);
            var card = Card.Create(_cardsNode, slot, cardState.Resource, cardState.CardId);
            
            card.GlobalPosition = GetViewportRect().Size + new Vector2(card.Size.X * 2, 0);

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
        float baselineY = GlobalPosition.Y;
        var count = _cardSlotsContainer.Count;

        var (positions, rotations) = _layoutCache.GetLayout(
            count,
            CardsCurveMultiplier,
            CardsRotationMultiplier,
            baselineY
        );

        for (int i = 0; i < count; i++)
        {
            var slot = _cardSlots[i];
            var card = slot.Card;

            // Handle Z-index ordering
            card.ZIndex = i;
            MoveChildSafe(card, i);

            slot.TargetPosition = new Vector2(slot.TargetPosition.X, positions[i].Y);
            card.Visual.RotationDegrees = rotations[i];
        }
    }

    private void HandleDrag()
    {
        try
        {
            var draggingCard = _cards.FirstOrDefault(card => _commandProcessor.CurrentState.Hand.DraggingCard?.CardId == card.CardId);
            if (draggingCard == null) return;

            var draggedCardSlot = draggingCard.Logic.CardSlot;
            if (draggedCardSlot == null) return;

            var validSlots = _cardSlots.Where(slot =>
                slot.GetCenter().DistanceTo(draggingCard.Logic.GetCenter()) <= draggedCardSlot.MaxValidDistance);

            if (!validSlots.Any())
                return;

            var validTargetSlot = validSlots.MinBy(slot =>
                slot.GetCenter().DistanceSquaredTo(draggingCard.Logic.GetCenter()));

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
        var draggedIndex = _cardSlots.IndexOf(draggedSlot);
        var targetIndex = _cardSlots.IndexOf(targetSlot);

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
}
