using Godot;

public class SpellCastState : IGameState
{
    public void OnEnter()
    {
        GD.Print("=== SPELL CAST ===");
    }

    public void OnExit()
    {
    }

    public IGameState HandleEvent(GameStateEvent turnEvent)
    {
        return turnEvent switch
        {
            GameStateEvent.SpellsComplete => new TurnEndState(),
            GameStateEvent.GameOver => new GameEndState(),
            _ => null // Invalid transition
        };
    }
}