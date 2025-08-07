using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Scripts.Commands;
using Scripts.Commands.Hand;
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
    private OrderedContainer _cardsContainer;
    private Node _cardsNode;
    private HandLayoutCache _layoutCache;
    private HandState _lastHandState;

    private ImmutableArray<Card> _cards => _cardsContainer
        ?.Where(n => n is Card)
        .Cast<Card>()
        .ToImmutableArray() ?? ImmutableArray<Card>.Empty;
    
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
        
        // Sort state cards by position to ensure correct order
        var orderedStateCards = currentState.Hand.Cards.OrderBy(c => c.Position).ToArray();
        var stateCardIds = orderedStateCards.Select(c => c.CardId).ToArray();
        _logger?.LogInfo($"[Hand] State card IDs (ordered): [{string.Join(", ", stateCardIds)}]");

        var toRemove = currentCards.Where(card => !orderedStateCards.Any(c => c.CardId == card.CardId)).ToList();
        var toAdd = orderedStateCards.Where(c => !currentCards.Any(card => card.CardId == c.CardId)).ToList();

        _logger?.LogInfo($"[Hand] Cards to remove: {toRemove.Count}, Cards to add: {toAdd.Count}");

        // Remove cards that are no longer in state
        foreach (var card in toRemove)
        {
            _logger?.LogInfo($"[Hand] Removing visual card {card.CardId} from hand");
            _cardsContainer.RemoveElement(card);
            card.QueueFree();
        }

        // Add new cards
        foreach (var cardState in toAdd)
        {
            _logger?.LogInfo($"[Hand] Adding visual card {cardState.CardId} to hand");
            CreateVisualCardFromState(cardState);
        }

        // Ensure visual cards are ordered according to state positions
        SyncCardOrder(orderedStateCards);
        
        var finalCards = _cards.ToArray();
        _logger?.LogInfo($"[Hand] Sync complete - Final visual cards: {finalCards.Length}");
    }

    private void SyncCardOrder(CardState[] orderedStateCards)
    {
        try
        {
            var currentCards = _cards.ToArray();
            bool orderChanged = false;

            // Check if the current visual order matches the state order
            for (int i = 0; i < orderedStateCards.Length && i < currentCards.Length; i++)
            {
                var stateCard = orderedStateCards[i];
                var visualCard = currentCards[i];
                
                if (visualCard.CardId != stateCard.CardId)
                {
                    orderChanged = true;
                    break;
                }
            }

            if (orderChanged || orderedStateCards.Length != currentCards.Length)
            {
                _logger?.LogInfo($"[Hand] Card order changed - reordering visual cards");
                
                // Clear container and re-add cards in correct order
                foreach (var card in currentCards)
                {
                    _cardsContainer.RemoveElement(card);
                }

                // Re-add cards in state order
                foreach (var stateCard in orderedStateCards)
                {
                    var visualCard = currentCards.FirstOrDefault(c => c.CardId == stateCard.CardId);
                    if (visualCard != null)
                    {
                        _cardsContainer.InsertElement(visualCard);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"[Hand] Error syncing card order: {ex.Message}", ex);
        }
    }
    
    private void CreateVisualCardFromState(CardState cardState)
    {
        try
        {
            _logger?.LogInfo($"[Hand] Creating visual card for state card {cardState.CardId}");
            
            // Create card directly
            var card = Card.Create(_cardsNode, cardState.Resource, cardState.CardId);
            
            // Set the card's initial position off-screen
            card.GlobalPosition = GetViewportRect().Size + new Vector2(card.Size.X * 2, 0);
            
            // Add card to the container (this triggers ElementsChanged and position calculation)
            _cardsContainer.InsertElement(card);
            
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
        _cardsContainer = GetNode<OrderedContainer>("CardsContainer").ValidateNotNull("CardsContainer");
    }

    private void SetupEventHandlers()
    {
        _cardsContainer.ElementsChanged += OnElementsChanged;
        _commandProcessor.StateChanged += OnHandStateChanged;
    }

    private void AdjustFanEffect()
    {
        try
        {
            var cards = _cards.ToArray();
            var count = cards.Length;
            
            if (count == 0) return; // No cards to adjust

            float baselineY = GlobalPosition.Y;
            var (positions, rotations) = _layoutCache.GetLayout(
                count,
                CardsCurveMultiplier,
                CardsRotationMultiplier,
                baselineY
            );

            for (int i = 0; i < count; i++)
            {
                var card = cards[i];

                // Skip if card is null (timing issue during creation)
                if (card == null)
                {
                    _logger?.LogWarning($"[Hand] Skipping null card at index {i}");
                    continue;
                }

                // Handle Z-index ordering
                card.ZIndex = i;

                // TargetPosition is set by OrderedContainer, just apply Y offset for fan effect
                var currentTarget = card.TargetPosition;
                card.TargetPosition = new Vector2(currentTarget.X, positions[i].Y);
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

            const float MAX_VALID_DISTANCE = 512f; // Same as the old CardSlot MaxValidDistance

            var otherCards = _cards.Where(card => card != draggingCard);
            var validCards = otherCards.Where(card => card.GetCenter().DistanceTo(draggingCard.GetCenter()) <= MAX_VALID_DISTANCE);

            if (!validCards.Any())
                return;

            var targetCard = validCards.MinBy(card => card.GlobalPosition.DistanceSquaredTo(draggingCard.GetCenter()));

            if (targetCard != null)
            {
                PerformCardReorder(draggingCard, targetCard);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling drag", ex);
        }
    }

    private void PerformCardReorder(Card draggedCard, Card targetCard)
    {
        var draggedIndex = _cardsContainer.IndexOf(draggedCard);
        var targetIndex = _cardsContainer.IndexOf(targetCard);

        if (draggedIndex < 0 || targetIndex < 0 || draggedIndex == targetIndex)
            return;

        // Get the new card order for the command
        var currentCards = _cards.ToList();
        var newCardOrder = new List<string>();
        
        // Create the new order by moving the dragged card to the target position
        var cardToMove = currentCards[draggedIndex];
        currentCards.RemoveAt(draggedIndex);
        
        // Adjust target index if dragged card was before target
        var adjustedTargetIndex = draggedIndex < targetIndex ? targetIndex - 1 : targetIndex;
        currentCards.Insert(adjustedTargetIndex, cardToMove);
        
        newCardOrder.AddRange(currentCards.Select(card => card.CardId));

        // Send the reorder command - this will update the state
        var reorderCommand = new ReorderCardsCommand(newCardOrder);
        var success = _commandProcessor.ExecuteCommand(reorderCommand);
        
        if (success)
        {
            _logger?.LogInfo($"[Hand] Successfully reordered cards - moved {draggedCard.CardId} to position {adjustedTargetIndex}");
        }
        else
        {
            _logger?.LogWarning($"[Hand] Failed to execute reorder command");
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
