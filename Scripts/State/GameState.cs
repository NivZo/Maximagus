using System;

namespace Scripts.State
{
    public class GameState : IGameStateData
    {
        public HandState Hand { get; }
        public PlayerState Player { get; }
        public GamePhaseState Phase { get; }
        public Guid StateId { get; }
        public DateTime CreatedAt { get; }

        private GameState(
            HandState hand,
            PlayerState player,
            GamePhaseState phase,
            Guid? stateId = null,
            DateTime? createdAt = null)
        {
            Hand = hand ?? throw new ArgumentNullException(nameof(hand));
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Phase = phase ?? throw new ArgumentNullException(nameof(phase));
            StateId = stateId ?? Guid.NewGuid();
            CreatedAt = createdAt ?? DateTime.UtcNow;
        }

        public static GameState CreateInitial()
        {
            return new GameState(
                new HandState(),
                new PlayerState(),
                new GamePhaseState()
            );
        }

        public static GameState Create(HandState hand, PlayerState player, GamePhaseState phase)
        {
            return new GameState(hand, player, phase);
        }

        public IGameStateData WithHand(HandState newHandState)
        {
            if (newHandState == null) throw new ArgumentNullException(nameof(newHandState));
            return new GameState(newHandState, Player, Phase);
        }

        public IGameStateData WithPlayer(PlayerState newPlayerState)
        {
            if (newPlayerState == null) throw new ArgumentNullException(nameof(newPlayerState));
            return new GameState(Hand, newPlayerState, Phase);
        }

        public IGameStateData WithPhase(GamePhaseState newPhaseState)
        {
            if (newPhaseState == null) throw new ArgumentNullException(nameof(newPhaseState));
            return new GameState(Hand, Player, newPhaseState);
        }

        public GameState WithComponents(
            HandState newHand = null,
            PlayerState newPlayer = null,
            GamePhaseState newPhase = null)
        {
            return new GameState(
                newHand ?? Hand,
                newPlayer ?? Player,
                newPhase ?? Phase
            );
        }

        public bool IsValid()
        {
            try
            {
                if (!Hand.IsValid() || !Player.IsValid() || !Phase.IsValid())
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
            return $"GameState[{StateId:N}] - Phase: {Phase.CurrentPhase}, " +
                   $"Turn: {Phase.TurnNumber}, Cards: {Hand.Count}, " +
                   $"Selected: {Hand.SelectedCount}, Health: {Player.Health}/{Player.MaxHealth}, " +
                   $"Hands: {Player.RemainingHands}/{Player.MaxHands}";
        }

        public GameState DeepCopy()
        {
            return new GameState(Hand, Player, Phase, StateId, CreatedAt);
        }

        public override bool Equals(object obj)
        {
            if (obj is GameState other)
            {
                return Hand.Equals(other.Hand) &&
                       Player.Equals(other.Player) &&
                       Phase.Equals(other.Phase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hand, Player, Phase);
        }

        public override string ToString()
        {
            return GetStateSummary();
        }
    }
}