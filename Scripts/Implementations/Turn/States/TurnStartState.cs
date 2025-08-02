using Godot;
using Maximagus.Scripts.Enums;

public class TurnStartState : IGameState
{
    private ILogger _logger;
    private IGameStateManager _gameStateManager;
    private IStatusEffectManager _statusEffectManager;

    public TurnStartState()
    {
        _logger = ServiceLocator.GetService<ILogger>();
        _gameStateManager = ServiceLocator.GetService<IGameStateManager>();
        _statusEffectManager = ServiceLocator.GetService<IStatusEffectManager>();
    }

    public void OnEnter()
    {
        GD.Print("=== TURN START ===");
        _statusEffectManager.TriggerEffects(StatusEffectTrigger.StartOfTurn);
        _gameStateManager.TriggerEvent(GameStateEvent.TurnStartEffectsComplete);
    }

    public void OnExit()
    {
    }

    public IGameState HandleEvent(GameStateEvent turnEvent)
    {
        return turnEvent switch
        {
            GameStateEvent.TurnStartEffectsComplete => new SubmitPhaseState(),
            GameStateEvent.GameOver => new GameEndState(),
            _ => null
        };
    }
}