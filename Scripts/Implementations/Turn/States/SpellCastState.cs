using Godot;
using Scripts.State;

public class SpellCastState : IGameState
{
    private ISpellProcessingManager _spellProcessingManager;

    public SpellCastState()
    {
        _spellProcessingManager = ServiceLocator.GetService<ISpellProcessingManager>();
    }

    public void OnEnter()
    {
        GD.Print("=== SPELL CAST ===");

        _spellProcessingManager.ProcessSpell();
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