using Godot;
using Maximagus.Scripts.Enums;

public class TurnStartState : IGameState
{
    private ILogger _logger;
    private IGameStateManager _gameStateManager;
    private IStatusEffectManager _statusEffectManager;
    private QueuedActionsManager _queuedActionsManager;

    public TurnStartState()
    {
        _logger = ServiceLocator.GetService<ILogger>();
        _gameStateManager = ServiceLocator.GetService<IGameStateManager>();
        _statusEffectManager = ServiceLocator.GetService<IStatusEffectManager>();
        _queuedActionsManager = ServiceLocator.GetService<QueuedActionsManager>();
    }

    public void OnEnter()
    {
        GD.Print("=== TURN START ===");
        _queuedActionsManager.QueueAction(() => _statusEffectManager.TriggerEffects(StatusEffectTrigger.StartOfTurn));
        _queuedActionsManager.QueueAction(() => _gameStateManager.TriggerEvent(GameStateEvent.TurnStartEffectsComplete));
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