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

    private OrderedContainer _cardSlotsContainer;

    public override void _Ready()
    {
        Instance = this;
        _cardSlotsContainer = GetNode<OrderedContainer>("CardSlotsContainer");
        _cardSlotsContainer.ElementsChanged += OnElementsChanged;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        var draggedCardSlot = CardSlots.FirstOrDefault(slot => slot.Card.CardLogic.IsDragging);
        var validSlots = CardSlots.Where(slot => slot.GlobalPosition.DistanceTo(draggedCardSlot.Card.CardLogic.GetCenter()) <= draggedCardSlot.MaxValidDistance);

        if (draggedCardSlot != null && validSlots.Any())
        {
            var validTargetSlot = validSlots.MinBy(slot => slot.GlobalPosition.DistanceSquaredTo(draggedCardSlot.Card.CardLogic.GetCenter()));

            if (validTargetSlot != null && validTargetSlot != draggedCardSlot)
            {
                _cardSlotsContainer.MoveElement(CardSlots.IndexOf(draggedCardSlot), CardSlots.IndexOf(validTargetSlot));
            }
        }
    }

    public void InitializeCardSlots()
    {
        GetChildren()
            .Where(n => n is CardSlot)
            .Select(n => n as CardSlot)
            .OrderBy(slot => slot.GlobalPosition.X)
            .Select((slot, i) => (slot, i))
            .ToList()
            .ForEach(indexedSlot => _cardSlotsContainer[indexedSlot.i] = indexedSlot.slot);

        CardSlotsChanged.Invoke();
    }
    
    private void OnElementsChanged()
    {
        foreach (var slot in CardSlots)
        {
            if (slot.Card != null)
            {
                slot.Card.ZIndex = CardSlots.IndexOf(slot);
            }
        }
    }
}
