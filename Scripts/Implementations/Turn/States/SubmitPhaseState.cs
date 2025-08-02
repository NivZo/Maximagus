using Godot;

public class SubmitPhaseState : IGameState
{
    public void OnEnter()
    {
        GD.Print("=== CARD SUBMIT ===");
    }

    public void OnExit()
    {
    }

    public IGameState HandleEvent(GameStateEvent turnEvent)
    {
        return turnEvent switch
        {
            GameStateEvent.HandDiscarded => new SubmitPhaseState(), // Loop back for redraw
            GameStateEvent.HandSubmitted => new SpellCastState(),
            _ => null // Invalid transition
        };
    }
}