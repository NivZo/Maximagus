using System;
using System.Linq;
using Scripts.State;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to play the currently selected cards as a spell
    /// </summary>
    public class PlayHandCommand : IGameCommand
    {
        public bool CanExecute(IGameStateData currentState)
        {
            if (currentState == null) return false;

            // Must be in spell casting phase
            if (!currentState.Phase.AllowsSpellCasting) return false;

            // Player must have hands remaining
            if (!currentState.Player.HasHandsRemaining) return false;

            // Must have at least one card selected
            if (currentState.Hand.SelectedCount == 0) return false;

            // Hand must not be locked
            if (currentState.Hand.IsLocked) return false;

            return true;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            Console.WriteLine("[PlayHandCommand] Execute() called!");
            
            // Get HandManager to access the Hand properly
            var handManager = ServiceLocator.GetService<IHandManager>();
            if (handManager?.Hand == null)
            {
                Console.WriteLine("[PlayHandCommand] ERROR: HandManager.Hand is null!");
                return currentState;
            }

            var selectedCards = handManager.Hand.SelectedCards;
            Console.WriteLine($"[PlayHandCommand] Playing {selectedCards.Length} selected cards");

            if (selectedCards.Length == 0)
            {
                Console.WriteLine("[PlayHandCommand] No cards selected!");
                return currentState;
            }

            // Execute the real game action through HandManager's Hand
            handManager.Hand.Discard(selectedCards);
            handManager.Hand.DrawAndAppend(selectedCards.Length);
            
            Console.WriteLine("[PlayHandCommand] Cards played and replaced successfully");

            // Also update GameState to keep it in sync
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.SpellResolution);
            var newPlayerState = currentState.Player.WithHandUsed();
            
            return currentState
                .WithPhase(newPhaseState)
                .WithPlayer(newPlayerState);
        }

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            // For undo, we need to restore the previous hand and player state
            // This is a complex undo that requires restoring multiple components
            return new RestoreGameStateCommand(previousState);
        }

        public string GetDescription()
        {
            return "Play selected cards as spell";
        }
    }

    /// <summary>
    /// Command to restore the complete game state (used for complex undos)
    /// </summary>
    public class RestoreGameStateCommand : IGameCommand
    {
        private readonly IGameStateData _targetState;

        public RestoreGameStateCommand(IGameStateData targetState)
        {
            _targetState = targetState ?? throw new ArgumentNullException(nameof(targetState));
        }

        public bool CanExecute(IGameStateData currentState)
        {
            // Can always restore to a valid previous state
            return _targetState.IsValid();
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            if (!CanExecute(currentState))
                throw new InvalidOperationException("Cannot execute RestoreGameStateCommand - target state is invalid");

            return _targetState;
        }

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            return new RestoreGameStateCommand(previousState);
        }

        public string GetDescription()
        {
            return $"Restore game state to: {_targetState.Phase.CurrentPhase}";
        }
    }
}