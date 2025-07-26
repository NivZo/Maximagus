using Godot;
using System;
using System.Linq;

public partial class CardSlot : Area2D, IOrderable
{
    [Export] public Card Card;
    public Vector2 TargetPosition
    {
        get => GlobalPosition;
        set
        {
            GlobalPosition = value;
            Card?.CardLogic.SetCardSlot(this);
        }
    }

    public Vector2 TargetSize
    {
        get => _shape.Size;
        set
        {
            if (value.X > 0 && value.Y > 0)
            {
                _shape.Size = value;
                Card?.CardLogic.SetCardSlot(this);
            }
        }
    }

    private RectangleShape2D _shape;


    public override void _Ready()
    {
        base._Ready();
        _shape = GetNode<CollisionShape2D>("CollisionShape2D").Shape as RectangleShape2D;
        AreaEntered += OnAreaEntered;
    }

    public void SetCard(Card card)
    {
        Card = card;
        Card?.CardLogic.SetCardSlot(this);
    }

    private void OnAreaEntered(Area2D area)
    {

    }
}
