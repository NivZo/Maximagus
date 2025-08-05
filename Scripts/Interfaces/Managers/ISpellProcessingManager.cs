using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;

public interface ISpellProcessingManager
{
    /// <summary>
    /// Process a spell using selected cards from the current GameState
    /// </summary>
    void ProcessSpell();
    
    /// <summary>
    /// Process spell with specific cards directly (compatibility bridge)
    /// </summary>
    void ProcessSpellWithCards(global::Card[] cards);
}