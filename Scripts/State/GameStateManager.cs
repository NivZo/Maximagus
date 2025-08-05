using System;
using System.Linq;
using Scripts.State;
using Maximagus.Scripts.Managers;

namespace Scripts.State
{
    /// <summary>
    /// Manages the game state as a single source of truth, keeping it synced with real game objects
    /// </summary>
    public class GameStateManager : IGameStateManager
    {
        private IGameStateData _currentState;

        public IGameStateData CurrentState => _currentState;

        public event Action<IGameStateData, IGameStateData> StateChanged;

        public GameStateManager()
        {
            _currentState = GameState.CreateInitial();
        }

        public void UpdateState(IGameStateData newState)
        {
            if (newState == null) throw new ArgumentNullException(nameof(newState));

            var previousState = _currentState;
            _currentState = newState;

            StateChanged?.Invoke(previousState, newState);
        }

        public void SyncWithRealGame()
        {
            try
            {
                // Get real game objects
                var handManager = ServiceLocator.GetService<IHandManager>();
                if (handManager?.Hand == null) return;

                var realHand = handManager.Hand;
                
                // Convert real cards to CardState objects
                var cardStates = realHand.Cards.Select(card => new CardState(
                    cardId: card.GetInstanceId().ToString(),
                    isSelected: card.IsSelected,
                    isDragging: card.IsDragging,
                    position: 0
                )).ToList();

                // Get selected card IDs
                var selectedCardIds = realHand.SelectedCards
                    .Select(card => card.GetInstanceId().ToString())
                    .ToList();

                // Create updated HandState with real data
                var newHandState = new HandState(
                    cards: cardStates,
                    selectedCardIds: selectedCardIds,
                    maxHandSize: 10,
                    isLocked: false
                );

                // Update the GameState with real data
                var newGameState = _currentState
                    .WithHand(newHandState);

                // Update state (this will fire StateChanged event)
                UpdateState(newGameState);
            }
            catch (Exception ex)
            {
                Godot.GD.PrintErr($"[GameStateManager] Error syncing with real game: {ex.Message}");
            }
        }
    }
}