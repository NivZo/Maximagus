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

    public override void _Ready()
    {
        try
        {
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
                _lastHandState = currentState.Hand;
                SyncVisualCardsWithState(currentState);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling hand state change", ex);
        }
    }
    
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
    
    private void SyncVisualCardsWithState(IGameStateData currentState)
    {
        var currentCards = _cards.ToArray();
        var orderedStateCards = currentState.Hand.Cards.OrderBy(c => c.Position).ToArray();

        var toRemove = currentCards.Where(card => !orderedStateCards.Any(c => c.CardId == card.CardId)).ToList();
        var toAdd = orderedStateCards.Where(c => !currentCards.Any(card => card.CardId == c.CardId)).ToList();

        foreach (var card in toRemove)
        {
            _cardsContainer.RemoveElement(card);
            card.QueueFree();
        }

        foreach (var cardState in toAdd)
        {
            CreateVisualCardFromState(cardState);
        }

        SyncCardOrder(orderedStateCards);
    }

    private void SyncCardOrder(CardState[] orderedStateCards)
    {
        try
        {
            var currentCards = _cards.ToArray();

            // Create a mapping of CardId to desired position from state
            var desiredOrder = orderedStateCards.Select((card, index) => new { card.CardId, DesiredIndex = index })
                                               .ToDictionary(x => x.CardId, x => x.DesiredIndex);

            // Move each card to its correct position using MoveElement
            for (int i = 0; i < currentCards.Length; i++)
            {
                var currentCard = currentCards[i];

                // Skip cards that are no longer in state (they should have been removed in SyncVisualCardsWithState)
                if (!desiredOrder.ContainsKey(currentCard.CardId))
                    continue;

                var desiredIndex = desiredOrder[currentCard.CardId];
                var currentIndex = _cardsContainer.IndexOf(currentCard);

                if (currentIndex != desiredIndex && currentIndex >= 0 && desiredIndex >= 0)
                {
                    _cardsContainer.MoveElement(currentIndex, desiredIndex);
                    // Update our local array to reflect the move for subsequent iterations
                    currentCards = _cards.ToArray();
                }
            }

            foreach (var (card, state) in currentCards.Zip(orderedStateCards))
            {
                _cardsNode.MoveChild(card, state.Position);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error syncing card order", ex);
        }
    }
    
    private void CreateVisualCardFromState(CardState cardState)
    {
        try
        {
            var card = Card.Create(_cardsNode, cardState.Resource, cardState.CardId);
            card.GlobalPosition = GetViewportRect().Size + new Vector2(card.Size.X * 2, -card.Size.Y * 3);
            _cardsContainer.InsertElement(card);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error creating visual card", ex);
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

                if (card == null)
                {
                    _logger?.LogWarning("Skipping null card");
                    continue;
                }

                card.ZIndex = i;
                var currentTarget = card.TargetPosition;
                card.TargetPosition = new Vector2(currentTarget.X, positions[i].Y);
                card.RotationDegrees = rotations[i];
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error in AdjustFanEffect", ex);
        }
    }

    private void HandleDrag()
    {
        try
        {
            var draggingCard = _cards.FirstOrDefault(card => _commandProcessor.CurrentState.Hand.DraggingCard?.CardId == card.CardId);
            if (draggingCard == null) return;

            var targetPositions = _cards.Select((card, index) => (card.TargetPosition, Index: index));
            var candidateIndex = targetPositions.MinBy(tp => tp.TargetPosition.DistanceSquaredTo(draggingCard.GetCenter())).Index;
            var draggedIndex = _cards.IndexOf(draggingCard);
            if (candidateIndex == draggedIndex) return;

            var candidateCard = _cards[candidateIndex];
            var newCardOrder = _cards.Select(card => card.CardId).ToArray();
            newCardOrder[candidateIndex] = draggingCard.CardId;
            newCardOrder[draggedIndex] = candidateCard.CardId;
            
            var reorderCommand = new ReorderCardsCommand(newCardOrder);
            var success = _commandProcessor.ExecuteCommand(reorderCommand);

            if (!success)
            {
                _logger?.LogWarning("Failed to execute reorder command");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling drag", ex);
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
