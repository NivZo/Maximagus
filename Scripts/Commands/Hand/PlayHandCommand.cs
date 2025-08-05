using System;
using System.Linq;
using Scripts.State;
using GlobalHand = Hand; // Alias to avoid namespace conflict

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
            Console.WriteLine("[PlayHandCommand] Execute() called - updating GameState!");
            
            if (!CanExecute(currentState))
                throw new InvalidOperationException("Cannot execute PlayHandCommand");

            // Get selected cards before they're removed
            var selectedCards = currentState.Hand.SelectedCards.ToList();
            Console.WriteLine($"[PlayHandCommand] Playing {selectedCards.Count} selected cards from GameState");

            // Remove selected cards from hand state
            var newHandState = currentState.Hand;
            foreach (var card in selectedCards)
            {
                newHandState = newHandState.WithRemovedCard(card.CardId);
            }

            // Use up one hand from player
            var newPlayerState = currentState.Player.WithHandUsed();

            // Transition to spell resolution phase
            var newPhaseState = currentState.Phase.WithPhase(GamePhase.SpellResolution);
            
            Console.WriteLine("[PlayHandCommand] GameState updated successfully");
            
            // Return new game state by chaining the with methods
            return currentState
                .WithHand(newHandState)
                .WithPlayer(newPlayerState)
                .WithPhase(newPhaseState);
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