using System;

namespace Scripts.State
{
    /// <summary>
    /// Immutable single source of truth for all game state.
    /// This is the core state object that contains all game data.
    /// </summary>
    public class GameState : IGameStateData
    {
        public HandState Hand { get; }
        public PlayerState Player { get; }
        public GamePhaseState Phase { get; }

        /// <summary>
        /// Unique identifier for this state version (for debugging and event sourcing)
        /// </summary>
        public Guid StateId { get; }

        /// <summary>
        /// Timestamp when this state was created
        /// </summary>
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

        /// <summary>
        /// Creates a new GameState with default initial values
        /// </summary>
        public static GameState CreateInitial()
        {
            return new GameState(
                new HandState(),
                new PlayerState(),
                new GamePhaseState()
            );
        }

        /// <summary>
        /// Creates a new GameState with specified values
        /// </summary>
        public static GameState Create(HandState hand, PlayerState player, GamePhaseState phase)
        {
            return new GameState(hand, player, phase);
        }

        /// <summary>
        /// Creates a new game state with updated hand state
        /// </summary>
        public IGameStateData WithHand(HandState newHandState)
        {
            if (newHandState == null) throw new ArgumentNullException(nameof(newHandState));
            return new GameState(newHandState, Player, Phase);
        }

        /// <summary>
        /// Creates a new game state with updated player state
        /// </summary>
        public IGameStateData WithPlayer(PlayerState newPlayerState)
        {
            if (newPlayerState == null) throw new ArgumentNullException(nameof(newPlayerState));
            return new GameState(Hand, newPlayerState, Phase);
        }

        /// <summary>
        /// Creates a new game state with updated phase state
        /// </summary>
        public IGameStateData WithPhase(GamePhaseState newPhaseState)
        {
            if (newPhaseState == null) throw new ArgumentNullException(nameof(newPhaseState));
            return new GameState(Hand, Player, newPhaseState);
        }

        /// <summary>
        /// Creates a new game state with multiple updated components
        /// </summary>
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

        /// <summary>
        /// Validates that the current state is consistent and valid
        /// </summary>
        public bool IsValid()
        {
            try
            {
                // Validate all component states
                if (!Hand.IsValid() || !Player.IsValid() || !Phase.IsValid())
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a summary of the current game state for debugging
        /// </summary>
        public string GetStateSummary()
        {
            return $"GameState[{StateId:N}] - Phase: {Phase.CurrentPhase}, " +
                   $"Turn: {Phase.TurnNumber}, Cards: {Hand.Count}, " +
                   $"Selected: {Hand.SelectedCount}, Health: {Player.Health}/{Player.MaxHealth}, " +
                   $"Hands: {Player.RemainingHands}/{Player.MaxHands}";
        }

        /// <summary>
        /// Creates a deep copy of this game state
        /// </summary>
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