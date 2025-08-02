using Godot;
using Maximagus.Scripts.Enums;

public class MenuState : IGameState
{
    private ILogger _logger;
    private IGameStateManager _gameStateManager;

    public MenuState()
    {
        _logger = ServiceLocator.GetService<ILogger>();
        _gameStateManager = ServiceLocator.GetService<IGameStateManager>();
    }

    public void OnEnter()
    {
        GD.Print("=== MENU ===");
    }

    public void OnExit()
    {
    }

    public IGameState HandleEvent(GameStateEvent turnEvent)
    {
        return turnEvent switch
        {
            GameStateEvent.StartGame => new TurnStartState(),
            _ => null
        };
    }
}