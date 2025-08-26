namespace Scripts.State
{

    public interface IGameStateData
    {

        CardsState Cards { get; }

        HandState Hand { get; }

        PlayerState Player { get; }

        GamePhaseState Phase { get; }

        SpellState Spell { get; }

        StatusEffectsState StatusEffects { get; }

        IGameStateData WithCards(CardsState newCardsState);

        IGameStateData WithHand(HandState newHandState);

        IGameStateData WithPlayer(PlayerState newPlayerState);

        IGameStateData WithPhase(GamePhaseState newPhaseState);

        IGameStateData WithSpell(SpellState newSpellState);

        IGameStateData WithStatusEffects(StatusEffectsState newStatusEffectsState);

        bool IsValid();

        string ToString();
    }
}