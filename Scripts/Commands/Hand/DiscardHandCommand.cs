using System;
using System.Linq;
using Scripts.State;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to discard the currently selected cards without playing them
    /// </summary>
    public class DiscardHandCommand : IGameCommand
    {
        public bool CanExecute(IGameStateData currentState)
        {
            if (currentState == null) return false;

            // Can discard during card selection or spell casting phases
            if (!currentState.Phase.AllowsCardSelection && !currentState.Phase.AllowsSpellCasting) 
                return false;

            // Must have at least one card selected
            if (currentState.Hand.SelectedCount == 0) return false;

            // Hand must not be locked
            if (currentState.Hand.IsLocked) return false;

            return true;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            if (!CanExecute(currentState))
                throw new InvalidOperationException("Cannot execute DiscardHandCommand");

            // Get selected cards before they're removed
            var selectedCards = currentState.Hand.SelectedCards.ToList();

            // Remove selected cards from hand
            var newHandState = currentState.Hand;
            foreach (var card in selectedCards)
            {
                newHandState = newHandState.WithRemovedCard(card.CardId);
            }

            // Return new game state with updated hand
            return currentState.WithHand(newHandState);
        }

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            // For undo, we need to restore the previous hand state
            return new RestoreHandStateCommand(previousState.Hand);
        }

        public string GetDescription()
        {
            return "Discard selected cards";
        }
    }

    /// <summary>
    /// Command to restore a specific hand state (used for hand-related undos)
    /// </summary>
    public class RestoreHandStateCommand : IGameCommand
    {
        private readonly HandState _targetHandState;

        public RestoreHandStateCommand(HandState targetHandState)
        {
            _targetHandState = targetHandState ?? throw new ArgumentNullException(nameof(targetHandState));
        }

        public bool CanExecute(IGameStateData currentState)
        {
            // Can always restore to a valid hand state
            return _targetHandState.IsValid();
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            if (!CanExecute(currentState))
                throw new InvalidOperationException("Cannot execute RestoreHandStateCommand - target hand state is invalid");

            return currentState.WithHand(_targetHandState);
        }

        public IGameCommand CreateUndoCommand(IGameStateData previousState)
        {
            return new RestoreHandStateCommand(previousState.Hand);
        }

        public string GetDescription()
        {
            return $"Restore hand state ({_targetHandState.Count} cards, {_targetHandState.SelectedCount} selected)";
        }
    }
}