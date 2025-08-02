using Godot;

public class GameEndState : IGameState
{
    public void OnEnter()
    {
        GD.Print("=== GAME OVER ===");
    }

    public void OnExit()
    {
    }

    public IGameState HandleEvent(GameStateEvent turnEvent)
    {
        return null;
    }
}