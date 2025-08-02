public interface IGameStateManager
{
    void StartGame();
    
    IGameState GetCurrentState();

    bool TriggerEvent(GameStateEvent turnEvent);
}