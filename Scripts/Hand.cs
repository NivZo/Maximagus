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
}
