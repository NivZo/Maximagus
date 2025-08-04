using System;
using Scripts.State;

namespace Scripts.State
{
    /// <summary>
    /// Builder pattern for constructing GameState objects with validation
    /// </summary>
    public class GameStateBuilder
    {
        private HandState _handState;
        private PlayerState _playerState;
        private GamePhaseState _phaseState;

        public GameStateBuilder()
        {
            // Initialize with default values
            _handState = new HandState();
            _playerState = new PlayerState();
            _phaseState = new GamePhaseState();
        }

        /// <summary>
        /// Creates a builder initialized with values from an existing game state
        /// </summary>
        /// <param name="existingState">The state to copy values from</param>
        /// <returns>New builder instance</returns>
        public static GameStateBuilder From(IGameStateData existingState)
        {
            if (existingState == null) throw new ArgumentNullException(nameof(existingState));

            return new GameStateBuilder()
                .WithHand(existingState.Hand)
                .WithPlayer(existingState.Player)
                .WithPhase(existingState.Phase);
        }

        /// <summary>
        /// Sets the hand state
        /// </summary>
        /// <param name="handState">The hand state to use</param>
        /// <returns>This builder for method chaining</returns>
        public GameStateBuilder WithHand(HandState handState)
        {
            _handState = handState ?? throw new ArgumentNullException(nameof(handState));
            return this;
        }

        /// <summary>
        /// Sets the player state
        /// </summary>
        /// <param name="playerState">The player state to use</param>
        /// <returns>This builder for method chaining</returns>
        public GameStateBuilder WithPlayer(PlayerState playerState)
        {
            _playerState = playerState ?? throw new ArgumentNullException(nameof(playerState));
            return this;
        }

        /// <summary>
        /// Sets the phase state
        /// </summary>
        /// <param name="phaseState">The phase state to use</param>
        /// <returns>This builder for method chaining</returns>
        public GameStateBuilder WithPhase(GamePhaseState phaseState)
        {
            _phaseState = phaseState ?? throw new ArgumentNullException(nameof(phaseState));
            return this;
        }

        /// <summary>
        /// Builds and validates the game state
        /// </summary>
        /// <returns>A valid GameState instance</returns>
        /// <exception cref="InvalidOperationException">Thrown if the resulting state would be invalid</exception>
        public GameState Build()
        {
            var gameState = GameState.Create(_handState, _playerState, _phaseState);

            if (!gameState.IsValid())
            {
                throw new InvalidOperationException(
                    $"Cannot build invalid game state. " +
                    $"Phase: {_phaseState.CurrentPhase}, " +
                    $"Player Health: {_playerState.Health}/{_playerState.MaxHealth}, " +
                    $"Hand Cards: {_handState.Count}, " +
                    $"Selected: {_handState.SelectedCount}");
            }

            return gameState;
        }

        /// <summary>
        /// Attempts to build the game state, returning null if invalid
        /// </summary>
        /// <returns>A valid GameState instance or null if invalid</returns>
        public GameState TryBuild()
        {
            try
            {
                return Build();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Validates the current builder state without creating the GameState
        /// </summary>
        /// <returns>True if the current configuration would result in a valid GameState</returns>
        public bool IsValid()
        {
            try
            {
                // Check individual component validity
                if (!_handState.IsValid() || !_playerState.IsValid() || !_phaseState.IsValid())
                    return false;

                // Create temporary state to check cross-component validation
                var tempState = GameState.Create(_handState, _playerState, _phaseState);
                return tempState.IsValid();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a summary of the current builder state for debugging
        /// </summary>
        /// <returns>String description of the current builder state</returns>
        public string GetStateSummary()
        {
            return $"GameStateBuilder - Phase: {_phaseState.CurrentPhase}, " +
                   $"Player: {_playerState.Health}/{_playerState.MaxHealth} HP, " +
                   $"Hand: {_handState.Count} cards ({_handState.SelectedCount} selected), " +
                   $"Valid: {IsValid()}";
        }

        /// <summary>
        /// Resets the builder to default values
        /// </summary>
        /// <returns>This builder for method chaining</returns>
        public GameStateBuilder Reset()
        {
            _handState = new HandState();
            _playerState = new PlayerState();
            _phaseState = new GamePhaseState();
            return this;
        }

        /// <summary>
        /// Creates a copy of this builder
        /// </summary>
        /// <returns>New builder instance with the same state</returns>
        public GameStateBuilder Clone()
        {
            return new GameStateBuilder()
                .WithHand(_handState)
                .WithPlayer(_playerState)
                .WithPhase(_phaseState);
        }
    }
}