using System;
using System.Linq;

namespace Scripts.State
{
    public class GameState : IGameStateData
    {
        public CardsState Cards { get; }
        public HandState Hand { get; }
        public PlayerState Player { get; }
        public GamePhaseState Phase { get; }
        public SpellState Spell { get; }
        public StatusEffectsState StatusEffects { get; }
        public Guid StateId { get; }
        public DateTime CreatedAt { get; }

        private GameState(
            CardsState cards,
            HandState hand,
            PlayerState player,
            GamePhaseState phase,
            SpellState spell,
            StatusEffectsState statusEffects,
            Guid? stateId = null,
            DateTime? createdAt = null)
        {
            Cards = cards ?? throw new ArgumentNullException(nameof(cards));
            Hand = hand ?? throw new ArgumentNullException(nameof(hand));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Phase = phase ?? throw new ArgumentNullException(nameof(phase));
            Spell = spell ?? throw new ArgumentNullException(nameof(spell));
            StatusEffects = statusEffects ?? throw new ArgumentNullException(nameof(statusEffects));
            StateId = stateId ?? Guid.NewGuid();
            CreatedAt = createdAt ?? DateTime.UtcNow;
        }

        public static GameState CreateInitial()
        {
            return new GameState(
                new CardsState(),
                new HandState(),
                new PlayerState(),
                new GamePhaseState(),
                SpellState.CreateInitial(),
                StatusEffectsState.CreateInitial()
            );
        }

        public static GameState Create(CardsState cards, HandState hand, PlayerState player, GamePhaseState phase, SpellState spell, StatusEffectsState statusEffects)
        {
            return new GameState(cards, hand, player, phase, spell, statusEffects);
        }

        public IGameStateData WithCards(CardsState newCardsState)
        {
            if (newCardsState == null) throw new ArgumentNullException(nameof(newCardsState));
            return new GameState(newCardsState, Hand, Player, Phase, Spell, StatusEffects);
        }

        public IGameStateData WithHand(HandState newHandState)
        {
            if (newHandState == null) throw new ArgumentNullException(nameof(newHandState));
            return new GameState(Cards, newHandState, Player, Phase, Spell, StatusEffects);
        }

        public IGameStateData WithPlayer(PlayerState newPlayerState)
        {
            if (newPlayerState == null) throw new ArgumentNullException(nameof(newPlayerState));
            return new GameState(Cards, Hand, newPlayerState, Phase, Spell, StatusEffects);
        }

        public IGameStateData WithPhase(GamePhaseState newPhaseState)
        {
            if (newPhaseState == null) throw new ArgumentNullException(nameof(newPhaseState));
            return new GameState(Cards, Hand, Player, newPhaseState, Spell, StatusEffects);
        }

        public IGameStateData WithSpell(SpellState newSpellState)
        {
            if (newSpellState == null) throw new ArgumentNullException(nameof(newSpellState));
            return new GameState(Cards, Hand, Player, Phase, newSpellState, StatusEffects);
        }

        public IGameStateData WithStatusEffects(StatusEffectsState newStatusEffectsState)
        {
            if (newStatusEffectsState == null) throw new ArgumentNullException(nameof(newStatusEffectsState));
            return new GameState(Cards, Hand, Player, Phase, Spell, newStatusEffectsState);
        }

        public GameState WithComponents(
            CardsState newCards = null,
            HandState newHand = null,
            PlayerState newPlayer = null,
            GamePhaseState newPhase = null,
            SpellState newSpell = null,
            StatusEffectsState newStatusEffects = null)
        {
            return new GameState(
                newCards ?? Cards,
                newHand ?? Hand,
                newPlayer ?? Player,
                newPhase ?? Phase,
                newSpell ?? Spell,
                newStatusEffects ?? StatusEffects
            );
        }

        public bool IsValid()
        {
            try
            {
                if (!Cards.IsValid() || !Hand.IsValid() || !Player.IsValid() || !Phase.IsValid())
                    return false;

                if (!Spell.IsValid() || !StatusEffects.IsValid())
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetStateSummary()
        {
            var selectedCount = Cards.SelectedInHand.Count();
            var inHandCount = Cards.InHandCount;

            return $"GameState[{StateId:N}] - Phase: {Phase.CurrentPhase}, " +
                   $"Turn: {Phase.TurnNumber}, CardsInHand: {inHandCount}, " +
                   $"Selected: {selectedCount}, Health: {Player.Health}/{Player.MaxHealth}, " +
                   $"Hands: {Player.RemainingHands}/{Player.MaxHands}, " +
                   $"SpellActive: {Spell.IsActive}, StatusEffects: {StatusEffects.TotalActiveEffects}";
        }

        public GameState DeepCopy()
        {
            return new GameState(Cards, Hand, Player, Phase, Spell, StatusEffects, StateId, CreatedAt);
        }

        public override bool Equals(object obj)
        {
            if (obj is GameState other)
            {
                return Cards.Equals(other.Cards) &&
                       Hand.Equals(other.Hand) &&
                       Player.Equals(other.Player) &&
                       Phase.Equals(other.Phase) &&
                       Spell.Equals(other.Spell) &&
                       StatusEffects.Equals(other.StatusEffects);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Cards, Hand, Player, Phase, Spell, StatusEffects);
        }

        public override string ToString()
        {
            return GetStateSummary();
        }
    }
}