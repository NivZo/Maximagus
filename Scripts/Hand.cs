using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

public partial class Hand : Control
{
    public static Hand Instance { get; private set; }
    public event Action CardSlotsChanged;
    public ImmutableArray<CardSlot> CardSlots => _cardSlotsContainer
        .Where(n => n is CardSlot)
        .Select(n => n as CardSlot)
        .ToImmutableArray();
    
    public ImmutableArray<Card> Cards => CardSlots
        .Select(slot => slot.Card)
        .Where(card => card != null)
        .ToImmutableArray();

    private OrderedContainer _cardSlotsContainer;
    private Node _cardsNode;
    private Node _cardSlotsNode;

    private bool _isShowingHover = false;

    public override void _Ready()
    {
        Instance = this;
        _cardsNode = GetNode<Node>("Cards");
        _cardSlotsNode = GetNode<Node>("CardSlots");
        _cardSlotsContainer = GetNode<OrderedContainer>("CardSlotsContainer");
        _cardSlotsContainer.ElementsChanged += OnElementsChanged;

        InitializeCardSlots();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        HandleDrag();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Left)
            {
                _cardSlotsContainer.RemoveElementAt(_cardSlotsContainer.Count);
            }
            else if (keyEvent.Keycode == Key.Right)
            {
                _cardSlotsContainer.InsertElement(_cardSlotsContainer.Count, CardSlot.Create(_cardSlotsNode));
            }
        }
    }

    public void InitializeCardSlots()
    {
        _cardSlotsNode.GetChildren()
            .Where(n => n is CardSlot)
            .Select(n => n as CardSlot)
            .OrderBy(slot => slot.GlobalPosition.X)
            .Select((slot, i) => (slot, i))
            .ToList()
            .ForEach(indexedSlot => _cardSlotsContainer[indexedSlot.i] = indexedSlot.slot);

        CardSlotsChanged?.Invoke();
        
        for (int i = 0; i < CardSlots.Length; i++)
        {
            Card.Create(_cardsNode, CardSlots[i]);
        }
    }
    private void HandleDrag()
    {
        var draggedCardSlot = CardSlots.FirstOrDefault(slot => slot.Card?.IsDragging ?? false);
        var validSlots = CardSlots.Where(slot => slot.GetCenter().DistanceTo(draggedCardSlot.Card.CardLogic.GetCenter()) <= draggedCardSlot.MaxValidDistance);

        if (draggedCardSlot != null && validSlots.Any())
        {
            var validTargetSlot = validSlots.MinBy(slot => slot.GetCenter().DistanceSquaredTo(draggedCardSlot.Card.CardLogic.GetCenter()));

            if (validTargetSlot != null && validTargetSlot != draggedCardSlot)
            {
                var draggedIndex = CardSlots.IndexOf(draggedCardSlot);
                var targetIndex = CardSlots.IndexOf(validTargetSlot);
                if (validTargetSlot.Card == null)
                {
                    _cardSlotsContainer.SwapElements(draggedIndex, targetIndex);
                }
                else
                {
                    _cardSlotsContainer.MoveElement(draggedIndex, targetIndex);
                }
            }
        }
    }

    private void OnElementsChanged()
    {
        foreach (var card in Cards)
        {
            var i = Cards.IndexOf(card);
            card.ZIndex = i;
            _cardsNode.MoveChild(card, i);
        }
    }
}
