using Godot;
using System;

public partial class Card : Control
{
	private static readonly string CARD_SCENE = "res://Scenes/Card/Card.tscn";

	public CardLogic CardLogic { get; private set; }
	public CardVisual CardVisual { get; private set; }

	public bool IsSelected => CardLogic.IsSelected;
	public bool IsDragging => CardLogic.IsDragging;
	public bool IsHovering => CardLogic.IsHovering;

	public override void _Ready()
	{
		base._Ready();

		GlobalPosition = Vector2.Zero;

		CardLogic = GetNode<CardLogic>("CardLogic");
		CardVisual = GetNode<CardVisual>("CardVisual");
		CardLogic.SetVisual(CardVisual);

		// TEMP
		CardVisual.GetNode<Label>("Label").Text = Name;
	}

	public override void _Process(double delta)
	{
		base._Process(delta);
	}

	public static Card Create(Node parent, CardSlot cardSlot)
	{
		if (cardSlot == null)
		{
			throw new ArgumentNullException(nameof(cardSlot), "CardSlot cannot be null.");
		}
		if (parent == null)
		{
			throw new ArgumentNullException(nameof(parent), "Parent cannot be null.");
		}

		var card = GD.Load<PackedScene>(CARD_SCENE).Instantiate<Card>();
		parent.AddChild(card);
		cardSlot.SetCard(card);
		return card;
	}
}
