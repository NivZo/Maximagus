using System;
using System.Linq;
using Scripts.State;
using Maximagus.Scripts.Managers;

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

            // Can discard during card selection phase
            if (!currentState.Phase.AllowsCardSelection)
                return false;

            // Must have at least one card selected
            if (currentState.Hand.SelectedCount == 0) return false;

            // Hand must not be locked
            if (currentState.Hand.IsLocked) return false;

            return true;
        }

        public IGameStateData Execute(IGameStateData currentState)
        {
            Console.WriteLine("[DiscardHandCommand] Execute() called!");
            
            // Get HandManager to access the Hand properly
            var handManager = ServiceLocator.GetService<IHandManager>();
            if (handManager?.Hand == null)
            {
                Console.WriteLine("[DiscardHandCommand] ERROR: HandManager.Hand is null!");
                return currentState;
            }

            var selectedCards = handManager.Hand.SelectedCards;
            Console.WriteLine($"[DiscardHandCommand] Discarding {selectedCards.Length} selected cards");

            if (selectedCards.Length == 0)
            {
                Console.WriteLine("[DiscardHandCommand] No cards selected!");
                return currentState;
            }

            // Execute the real game action through HandManager's Hand
            handManager.Hand.Discard(selectedCards);
            handManager.Hand.DrawAndAppend(selectedCards.Length);
            
            Console.WriteLine("[DiscardHandCommand] Cards discarded and replaced successfully");

            // GameState remains unchanged for discard (just removes and replaces cards)
            return currentState;
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