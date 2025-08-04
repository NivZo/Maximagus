using Godot.Collections;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Spells.Abstractions;

public interface IHandManager
{
    void ResetForNewEncounter();

    bool CanSubmitHand(HandActionType actionType);
}