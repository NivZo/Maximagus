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
        public Guid StateId { get; }
        public DateTime CreatedAt { get; }

        private GameState(
            CardsState cards,
            HandState hand,
            PlayerState player,
            GamePhaseState phase,
            Guid? stateId = null,
            DateTime? createdAt = null)
        {
            Cards = cards ?? throw new ArgumentNullException(nameof(cards));
            Hand = hand ?? throw new ArgumentNullException(nameof(hand));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Phase = phase ?? throw new ArgumentNullException(nameof(phase));
            StateId = stateId ?? Guid.NewGuid();
            CreatedAt = createdAt ?? DateTime.UtcNow;
        }

        public static GameState CreateInitial()
        {
            return new GameState(
                new CardsState(),
                new HandState(),
                new PlayerState(),
                new GamePhaseState()
            );
        }

        public static GameState Create(CardsState cards, HandState hand, PlayerState player, GamePhaseState phase)
        {
            return new GameState(cards, hand, player, phase);
        }

        public IGameStateData WithCards(CardsState newCardsState)
        {
            if (newCardsState == null) throw new ArgumentNullException(nameof(newCardsState));
            return new GameState(newCardsState, Hand, Player, Phase);
        }

        public IGameStateData WithHand(HandState newHandState)
        {
            if (newHandState == null) throw new ArgumentNullException(nameof(newHandState));
            return new GameState(Cards, newHandState, Player, Phase);
        }

        public IGameStateData WithPlayer(PlayerState newPlayerState)
        {
            if (newPlayerState == null) throw new ArgumentNullException(nameof(newPlayerState));
            return new GameState(Cards, Hand, newPlayerState, Phase);
        }

        public IGameStateData WithPhase(GamePhaseState newPhaseState)
        {
            if (newPhaseState == null) throw new ArgumentNullException(nameof(newPhaseState));
            return new GameState(Cards, Hand, Player, newPhaseState);
        }

        public GameState WithComponents(
            CardsState newCards = null,
            HandState newHand = null,
            PlayerState newPlayer = null,
            GamePhaseState newPhase = null)
        {
            return new GameState(
                newCards ?? Cards,
                newHand ?? Hand,
                newPlayer ?? Player,
                newPhase ?? Phase
            );
        }

        public bool IsValid()
        {
            try
            {
                if (!Cards.IsValid() || !Hand.IsValid() || !Player.IsValid() || !Phase.IsValid())
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
                   $"Hands: {Player.RemainingHands}/{Player.MaxHands}";
        }

        public GameState DeepCopy()
        {
            return new GameState(Cards, Hand, Player, Phase, StateId, CreatedAt);
        }

        public override bool Equals(object obj)
        {
            if (obj is GameState other)
            {
                return Cards.Equals(other.Cards) &&
                       Hand.Equals(other.Hand) &&
                       Player.Equals(other.Player) &&
                       Phase.Equals(other.Phase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Cards, Hand, Player, Phase);
        }

        public override string ToString()
        {
            return GetStateSummary();
        }
    }
}