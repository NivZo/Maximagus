using Godot.Collections;
using Maximagus.Scripts.Spells.Abstractions;

public interface ISpellProcessingManager
{
    void ProcessSpell(Array<SpellCardResource> cards);
}