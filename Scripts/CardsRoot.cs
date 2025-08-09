using Godot;
using Scripts.Commands;
using Scripts.State;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class CardsRoot : Node
{
    public Card[] Cards => GetChildren().OfType<Card>().ToArray();
    public Card Create(CardState cardState)
    {
        var exist = Cards.FirstOrDefault(c => c.CardId == cardState.CardId);
        if (exist != null) return exist;

        var card = Card.Create(cardState.Resource, cardState.CardId);
        AddChild(card);
        return card;
    }
}
