public interface IGameState
{
    void OnEnter();
    void OnExit();
    IGameState HandleEvent(GameStateEvent gameStateEvent);
}