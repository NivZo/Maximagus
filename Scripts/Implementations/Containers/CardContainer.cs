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

public abstract partial class CardContainer : Control
{
    [Export] public float CardsCurveMultiplier = GameConfig.DEFAULT_CARDS_CURVE_MULTIPLIER;
    [Export] public float CardsRotationMultiplier = GameConfig.DEFAULT_CARDS_ROTATION_MULTIPLIER;

    private ILogger _logger;
    private IGameCommandProcessor _commandProcessor;
    private OrderedContainer _cardsContainer;
    private CardsRoot _cardsRoot;
    private ContainerLayoutCache _layoutCache;
    private CardState[] _lastCardsState;

    public ImmutableArray<Card> Cards => _cardsContainer
        ?.Where(n => n is Card)
        .Cast<Card>()
        .ToImmutableArray() ?? ImmutableArray<Card>.Empty;

    public override void _Ready()
    {
        try
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
            _cardsRoot = ServiceLocator.GetService<CardsRoot>();
            _layoutCache = new ContainerLayoutCache();

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

    public abstract CardState[] GetCardStates(IGameStateData currentState);

    public virtual void OnCardEnter(Card card) { }

    public void MoveToContainer(Card card, CardContainer targetContainer)
    {
        _cardsContainer.RemoveElement(card);
        targetContainer._cardsContainer.InsertElement(card);
    }

    private void InitializeComponents()
    {
        _cardsContainer = GetNode<OrderedContainer>("CardsContainer").ValidateNotNull("CardsContainer");
    }

    private void SetupEventHandlers()
    {
        _cardsContainer.ElementsChanged += OnElementsChanged;
        _commandProcessor.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(IGameStateData oldGlobalState, IGameStateData newGlobalState)
    {
        var newState = GetCardStates(newGlobalState);
        try
        {
            // Use more reliable comparison - check if the actual card lists differ
            bool containerStateChanged = _lastCardsState == null ||
                                   _lastCardsState.Length != newState.Length ||
                                   !CardsAreEqual(_lastCardsState, newState);

            if (containerStateChanged)
            {
                _lastCardsState = newState;
                SyncVisualCardsWithState(_lastCardsState);
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
    
    private void SyncVisualCardsWithState(CardState[] currentState)
    {
        var currentCards = Cards.ToArray();
        var orderedStateCards = currentState.OrderBy(c => c.Position).ToArray();

        var toRemove = currentCards.Where(card => !orderedStateCards.Any(c => c.CardId == card.CardId)).ToList();
        var toAdd = orderedStateCards.Where(c => !currentCards.Any(card => card.CardId == c.CardId)).ToList();

        foreach (var card in toRemove)
        {
            _cardsContainer.RemoveElement(card);
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
            var currentCards = Cards.ToArray();

            var desiredOrder = orderedStateCards
                .Select((card, index) => new { card.CardId, DesiredIndex = index })
                .ToDictionary(x => x.CardId, x => x.DesiredIndex);

            for (int i = 0; i < _cardsContainer.Count; i++)
            {
                var card = _cardsContainer[i] as Card;
                var desiredIndex = desiredOrder.GetValueOrDefault(card?.CardId, _cardsContainer.Count);
                if (desiredIndex != i)
                {
                    _cardsContainer.MoveElement(i, desiredIndex);
                    _cardsRoot.MoveChild(card, desiredIndex);
                    card.ZIndex = desiredIndex;
                }
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
            var card = _cardsRoot.Create(cardState);
            OnCardEnter(card);
            _cardsContainer.InsertElement(card);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error creating visual card", ex);
        }
    }

    private void AdjustTargetPositions()
    {
        try
        {
            var cards = Cards.ToArray();
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
            var draggingState = _lastCardsState.FirstOrDefault(card => card.IsDragging);
            var draggingCard = draggingState == null ? null : Cards.FirstOrDefault(card => draggingState.CardId == card.CardId);
            if (draggingCard == null) return;

            var targetPositions = Cards.Select((card, index) => (card.TargetPosition, Index: index));
            var candidateIndex = targetPositions.MinBy(tp => tp.TargetPosition.DistanceSquaredTo(draggingCard.GetCenter())).Index;
            var draggedIndex = Cards.IndexOf(draggingCard);
            if (candidateIndex == draggedIndex) return;

            var candidateCard = Cards[candidateIndex];
            var newCardOrder = Cards.Select(card => card.CardId).ToArray();
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
            AdjustTargetPositions();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling elements changed", ex);
        }
    }
}
