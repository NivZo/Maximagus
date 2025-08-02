using System;
using Godot;
using Maximagus.Scripts.Events;

public enum GameStateEvent
{
    StartGame,
    TurnStartEffectsComplete,
    HandDiscarded,
    HandSubmitted,
    SpellsComplete,
    TurnEndEffectsComplete,
    GameOver
}

public class GameStateManager : IGameStateManager
{
    private IEventBus _eventBus;
    private IGameState _currentState;
    
    public IGameState CurrentState => _currentState;

    public GameStateManager()
    {
        _eventBus = ServiceLocator.GetService<IEventBus>();
    }

    public void StartGame()
    {
        _currentState = new MenuState();
        _currentState.OnEnter();
    }

    public IGameState GetCurrentState() => _currentState;

    public bool TriggerEvent(GameStateEvent gameStateEvent)
    {
        var nextState = _currentState.HandleEvent(gameStateEvent);

        if (nextState == null)
        {
            return false;
        }

        if (nextState != _currentState)
        {
            TransitionToState(nextState);
        }

        return true;
    }

    private void TransitionToState(IGameState newState)
    {
        var previousState = _currentState;

        _currentState.OnExit();
        _currentState = newState;
        _currentState.OnEnter();

        _eventBus.Publish(new GameStateChangedEvent()
        {
            PreviousState = previousState,
            NewState = _currentState,
        });
    }

    public void Reset()
    {
        TransitionToState(new TurnStartState());
    }
}