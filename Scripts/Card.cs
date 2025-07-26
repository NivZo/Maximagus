using Godot;
using System;

public partial class Card : Control
{
	private static readonly string CardScene = "res://Scenes/Card/Card.tscn";

	public CardLogic CardLogic { get; private set; }
	public CardVisual CardVisual { get; private set; }

	public override void _Ready()
	{
		base._Ready();

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

		var card = GD.Load<PackedScene>(CardScene).Instantiate<Card>();
		parent.AddChild(card);
		cardSlot.Card = card;
		card.CardLogic.SetCardSlot(cardSlot);
		card.CardLogic.InvokePositionChanged();
		return card;
	}
}
