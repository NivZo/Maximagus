using Godot;
using Maximagus.Scripts.Enums;

public class TurnEndState : IGameState
{
    private ILogger _logger;
    private IGameStateManager _gameStateManager;
    private IStatusEffectManager _statusEffectManager;

    public TurnEndState()
    {
        _logger = ServiceLocator.GetService<ILogger>();
        _gameStateManager = ServiceLocator.GetService<IGameStateManager>();
        _statusEffectManager = ServiceLocator.GetService<IStatusEffectManager>();
    }

    public void OnEnter()
    {
        GD.Print("=== TURN END ===");
        _statusEffectManager.TriggerEffects(StatusEffectTrigger.EndOfTurn);
        _gameStateManager.TriggerEvent(GameStateEvent.TurnEndEffectsComplete);
    }

    public void OnExit()
    {
    }

    public IGameState HandleEvent(GameStateEvent turnEvent)
    {
        return turnEvent switch
        {
            GameStateEvent.TurnEndEffectsComplete => new TurnStartState(),
            GameStateEvent.GameOver => new GameEndState(),
            _ => null
        };
    }
}