using Godot;

public class CardDragStartedEvent
{
    public Card Card { get; }
    public CardDragStartedEvent(Card card) => Card = card;
}

public class CardDragEndedEvent
{
    public Card Card { get; }
    public CardDragEndedEvent(Card card) => Card = card;
}

public class CardHoverStartedEvent
{
    public Card Card { get; }
    public CardHoverStartedEvent(Card card) => Card = card;
}

public class CardHoverEndedEvent
{
    public Card Card { get; }
    public CardHoverEndedEvent(Card card) => Card = card;
}

public class CardClickedEvent
{
    public Card Card { get; }
    public CardClickedEvent(Card card) => Card = card;
}

public class CardPositionChangedEvent
{
    public Card Card { get; }
    public float Delta { get; }
    public Vector2 Position { get; }
    public bool IsDueToDragging { get; }
    
    public CardPositionChangedEvent(Card card, float delta, Vector2 position, bool isDueToDragging)
    {
        Card = card;
        Delta = delta;
        Position = position;
        IsDueToDragging = isDueToDragging;
    }
}