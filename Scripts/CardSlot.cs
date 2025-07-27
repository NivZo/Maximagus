using Godot;
using System;
using System.Linq;

public partial class CardSlot : Control, IOrderable
{
	private static readonly string CARD_SCENE = "res://Scenes/Card/CardSlot.tscn";

    [Export] public float MaxValidDistance = 248;

    public Card Card { get; private set; }

    public Vector2 TargetPosition
    {
        get => GlobalPosition;
        set
        {
            GlobalPosition = value;
            Card?.CardLogic.SetCardSlot(this);
        }
    }

    public Vector2 Weight => Vector2.One;

    public int Z
    {
        get => ZIndex;
        set => ZIndex = value;
    }

    public static CardSlot Create(Node parent = null)
    {
		var cardSlot = GD.Load<PackedScene>(CARD_SCENE).Instantiate<CardSlot>();
		parent?.AddChild(cardSlot);
		return cardSlot;
    }

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        // if (Card.IsDragging)
        // {
        //     ReorderSlots();
        // }
    }

    public void SetCard(Card card)
    {
        Card = card;

        if (Card == null) return;

        Card.CardLogic.SetCardSlot(this);
    }

    private static void SwitchSlotContents(CardSlot slotA, CardSlot slotB)
    {
        var cardA = slotA.Card;

        slotA.SetCard(slotB.Card);
        slotB.SetCard(cardA);
    }
}
