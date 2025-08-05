using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Maximagus.Scripts.Spells.Abstractions;
using Scripts.Commands;
using Scripts.Commands.Hand;
using Scripts.Config;
using Scripts.Utils;

public partial class Hand : Control
{
    public static Hand Instance { get; private set; }

    [Export] float CardsCurveMultiplier = GameConfig.DEFAULT_CARDS_CURVE_MULTIPLIER;
    [Export] float CardsRotationMultiplier = GameConfig.DEFAULT_CARDS_ROTATION_MULTIPLIER;

    private IEventBus _eventBus;
    private ILogger _logger;
    private IGameCommandProcessor _commandProcessor;
    private Deck _deck;
    private OrderedContainer _cardSlotsContainer;
    private Node _cardsNode;
    private Node _cardSlotsNode;
    private HandLayoutCache _layoutCache;

    private ImmutableArray<CardSlot> _cardSlots => _cardSlotsContainer
        ?.Where(n => n is CardSlot)
        .Cast<CardSlot>()
        .ToImmutableArray() ?? ImmutableArray<CardSlot>.Empty;

    private ImmutableArray<Card> _cards => _cardSlots
        .Select(slot => slot.Card)
        .Where(card => card != null)
        .ToImmutableArray();

    private ImmutableArray<Card> _selectedCards
    {
        get
        {
            if (_commandProcessor?.CurrentState == null)
                return ImmutableArray<Card>.Empty;

            var selectedCardIds = _commandProcessor.CurrentState.Hand.SelectedCardIds;
            return _cards
                .Where(card => selectedCardIds.Contains(card.GetInstanceId().ToString()))
                .ToImmutableArray();
        }
    }

    private Card _draggingCard
    {
        get
        {
            if (_commandProcessor?.CurrentState == null)
                return null;

            var draggingCardState = _commandProcessor.CurrentState.Hand.DraggingCard;
            if (draggingCardState == null) return null;

            return _cards.FirstOrDefault(card =>
                card.GetInstanceId().ToString() == draggingCardState.CardId);
        }
    }

    public ImmutableArray<CardSlot> CardSlots => _cardSlots;
    public ImmutableArray<Card> Cards => _cards;
    public ImmutableArray<Card> SelectedCards => _selectedCards;
    public Card DraggingCard => _draggingCard;

    public override void _Ready()
    {
        try
        {
            Instance = this;
            _eventBus = ServiceLocator.GetService<IEventBus>();
            _logger = ServiceLocator.GetService<ILogger>();

            // TIMING FIX: Don't get CommandProcessor here - it might not be registered yet
            // We'll get it when we need it in DrawAndAppend()
            _deck = new();
            _layoutCache = new HandLayoutCache();

            SubscribeToEvents();
            InitializeComponents();
            SetupEventHandlers();
            InitializeCardSlots();
            TryGetCommandProcessor();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error initializing Hand", ex);
            throw;
        }
    }

    /// <summary>
    /// Try to get the CommandProcessor if we don't have it yet
    /// </summary>
    private bool TryGetCommandProcessor()
    {
        if (_commandProcessor != null) return true;

        _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        if (_commandProcessor != null)
        {
            GD.Print("[Hand] CommandProcessor obtained from ServiceLocator");
            return true;
        }

        GD.PrintErr("[Hand] CommandProcessor still not available in ServiceLocator");
        return false;
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

            // Try to get CommandProcessor if we don't have it yet
            if (_commandProcessor == null)
            {
                // TryGetCommandProcessor();
            }

            HandleDrag(); // PURE COMMAND SYSTEM: Now uses GameState for drag detection
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

            // Create cards for each slot - keeping original behavior to avoid null reference errors
            var rnd = new Random();
            foreach (var slot in _cardSlots)
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
            if (_cards.Contains(card))
            {
                var cardId = card.GetInstanceId().ToString();
                GD.Print($"[Hand] Discarding card {card.Resource.CardName} (ID: {cardId}) from slot {_cardSlotsContainer.IndexOf(card.Logic.CardSlot) + 1}");

                // CRITICAL: Remove card from GameState first
                if (_commandProcessor != null)
                {
                    var removeCardCommand = new RemoveCardCommand(cardId);
                    var success = _commandProcessor.ExecuteCommand(removeCardCommand);

                    if (success)
                    {
                        GD.Print($"[Hand] SUCCESS: Removed card {cardId} from GameState");
                    }
                    else
                    {
                        GD.PrintErr($"[Hand] FAILED: Could not remove card {cardId} from GameState");
                    }
                }
                else
                {
                    GD.PrintErr($"[Hand] CommandProcessor not available - card {cardId} removed visually but still in GameState!");
                }

                // Remove visual card
                _cardSlotsContainer.RemoveElement(card.Logic.CardSlot);
                card.Logic.CardSlot.QueueFree();
                card.QueueFree();
            }
        }
    }

    public void DrawAndAppend(int amount)
    {
        GD.Print($"[Hand] DrawAndAppend called for {amount} cards");

        var currentState = _commandProcessor.CurrentState;
        if (currentState == null) return;

        GD.Print($"[Hand] Current hand size in GameState: {currentState.Hand.Count}/{currentState.Hand.MaxHandSize}");
        GD.Print($"[Hand] Hand locked: {currentState.Hand.IsLocked}");

        // CRITICAL: Check if we have space for new cards
        if (currentState.Hand.Count >= currentState.Hand.MaxHandSize)
        {
            GD.PrintErr($"[Hand] ERROR: GameState hand is full ({currentState.Hand.Count}/{currentState.Hand.MaxHandSize}) - cannot add more cards!");
            GD.PrintErr($"[Hand] Visual cards: {_cards.Length}, GameState cards: {currentState.Hand.Count}");
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            if (currentState.Hand.Count >= currentState.Hand.MaxHandSize)
            {
                GD.PrintErr($"[Hand] Hand full after adding {i} cards - stopping");
                break;
            }

            var resource = _deck.GetNext();
            var slot = CardSlot.Create(_cardSlotsNode);
            var card = Card.Create(_cardsNode, slot, resource);
            card.GlobalPosition = GetViewportRect().Size + new Vector2(card.Size.X * 2, 0);
            _cardSlotsContainer.InsertElement(slot);

            var cardId = card.GetInstanceId().ToString();
            GD.Print($"[Hand] Created visual card {card.Resource.CardName} with ID {cardId}");

            // CRITICAL: Add card to GameState so it's available for commands
            var addCardCommand = new AddCardCommand(cardId);

            // Check if command can execute
            if (!addCardCommand.CanExecute(currentState))
            {
                GD.PrintErr($"[Hand] AddCardCommand.CanExecute FAILED for card {cardId}!");
                GD.PrintErr($"[Hand] Current state: Hand={currentState.Hand.Count}/{currentState.Hand.MaxHandSize}, Locked={currentState.Hand.IsLocked}");

                // Check specific failure reasons
                if (currentState.Hand.IsLocked)
                {
                    GD.PrintErr($"[Hand] REASON: Hand is locked");
                }
                if (currentState.Hand.Count >= currentState.Hand.MaxHandSize)
                {
                    GD.PrintErr($"[Hand] REASON: Hand size limit reached ({currentState.Hand.Count}/{currentState.Hand.MaxHandSize})");
                }

                // Check if card already exists (shouldn't happen with new cards)
                var cardExists = false;
                foreach (var existingCard in currentState.Hand.Cards)
                {
                    if (existingCard.CardId == cardId)
                    {
                        cardExists = true;
                        break;
                    }
                }
                if (cardExists)
                {
                    GD.PrintErr($"[Hand] REASON: Card {cardId} already exists in GameState");
                }

                continue; // Skip this card
            }

            var success = _commandProcessor.ExecuteCommand(addCardCommand);

            if (success)
            {
                GD.Print($"[Hand] SUCCESS: Added card {card.Resource.CardName} (ID: {cardId}) to GameState and slot {_cardSlotsContainer.IndexOf(slot) + 1}");

                // Update current state reference for next iteration
                currentState = _commandProcessor.CurrentState;
            }
            else
            {
                GD.PrintErr($"[Hand] FAILED: ExecuteCommand returned false for card {cardId} - card will not be selectable!");
            }
        }

        // Final state check
        var finalState = _commandProcessor.CurrentState;
        GD.Print($"[Hand] DrawAndAppend complete. Final hand size: {finalState?.Hand.Count}/{finalState?.Hand.MaxHandSize}");
    }

    /// <summary>
    /// Sync any visual cards that weren't added to GameState (called when CommandProcessor becomes available)
    /// </summary>
    public void SyncVisualCardsToGameState()
    {
        if (_commandProcessor == null) return;

        GD.Print("[Hand] Syncing visual cards to GameState...");

        var currentState = _commandProcessor.CurrentState;
        if (currentState == null) return;

        // Find cards that exist visually but not in GameState
        foreach (var card in _cards)
        {
            var cardId = card.GetInstanceId().ToString();
            var existsInGameState = currentState.Hand.Cards.Any(c => c.CardId == cardId);

            if (!existsInGameState)
            {
                GD.Print($"[Hand] Found visual card {cardId} not in GameState - adding it");

                var addCardCommand = new AddCardCommand(cardId);
                if (addCardCommand.CanExecute(currentState))
                {
                    var success = _commandProcessor.ExecuteCommand(addCardCommand);
                    if (success)
                    {
                        GD.Print($"[Hand] Successfully synced card {cardId} to GameState");
                        currentState = _commandProcessor.CurrentState; // Update for next iteration
                    }
                    else
                    {
                        GD.PrintErr($"[Hand] Failed to sync card {cardId} to GameState");
                    }
                }
                else
                {
                    GD.PrintErr($"[Hand] Cannot sync card {cardId} - hand may be full or locked");
                }
            }
        }
    }

    private void AdjustFanEffect()
    {
        float baselineY = GlobalPosition.Y;
        var count = _cardSlotsContainer.Count;

        // PERFORMANCE OPTIMIZATION: Use cached layout calculations
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

            // Skip empty slots (when no cards are present)
            if (card == null) continue;

            // Handle Z-index ordering
            card.ZIndex = i;
            _cardsNode.MoveChild(card, i);

            // PERFORMANCE OPTIMIZATION: Use cached position and rotation
            slot.TargetPosition = new Vector2(slot.TargetPosition.X, positions[i].Y);
            card.Visual.RotationDegrees = rotations[i];
        }
    }

    /// <summary>
    /// PURE COMMAND SYSTEM: Handle drag using GameState instead of legacy IsDragging properties
    /// </summary>
    private void HandleDrag()
    {
        try
        {
            // Get dragging card from GameState instead of checking each card's IsDragging property
            var draggingCard = _draggingCard;
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

    /// <summary>
    /// KEEPING: Legacy slot reordering per instructions (to be replaced in future phase)
    /// </summary>
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

            // TIMING FIX: Try to sync any unsynced cards when elements change
            if (_commandProcessor != null)
            {
                CallDeferred(MethodName.SyncVisualCardsToGameState);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling elements changed", ex);
        }
    }
}
