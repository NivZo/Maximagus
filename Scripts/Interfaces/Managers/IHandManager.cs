using Maximagus.Scripts.Enums;

public interface IHandManager
{
    Hand Hand { get; }

    void SetupHandNode(Hand hand);

    void ResetForNewEncounter();

    bool CanSubmitHand(HandActionType actionType);
}