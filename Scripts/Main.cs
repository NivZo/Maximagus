using Godot;
using System;

public partial class Main : Control
{
    public static Main Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        Hand.Instance.CardSlotsChanged += () =>
        {
            Card.Create(this, Hand.Instance.CardSlots[0]);
            Card.Create(this, Hand.Instance.CardSlots[1]);
            Card.Create(this, Hand.Instance.CardSlots[2]);
            Card.Create(this, Hand.Instance.CardSlots[3]);
        };
        Hand.Instance.InitializeCardSlots();
    }

    public override void _Process(double delta)
    {
    }
}
