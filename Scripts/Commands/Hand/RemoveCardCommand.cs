using Scripts.State;
using Godot;
using System.Linq;

namespace Scripts.Commands.Hand
{
    /// <summary>
    /// Command to remove a card from the player's hand in GameState
    /// Used when cards are discarded or removed
    /// </summary>
    public class RemoveCardCommand : GameCommand
    {
        private readonly string _cardId;

        public RemoveCardCommand(string cardId) : base()
        {
            _cardId = cardId;
        }

        public override bool CanExecute()
        {
            if (_commandProcessor.CurrentState == null) return false;
            if (string.IsNullOrEmpty(_cardId)) return false;
            if (_commandProcessor.CurrentState.Hand.IsLocked) return false;

            // Card must exist
            return _commandProcessor.CurrentState.Cards.Cards.Any(c => c.CardId == _cardId);
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            GD.Print($"[RemoveCardCommand] Removing card {_cardId} from GameState");

            var newCards = currentState.Cards.WithRemovedCard(_cardId);
            var newState = currentState.WithCards(newCards);

            GD.Print($"[RemoveCardCommand] Card {_cardId} removed from GameState successfully");

            token.Complete(CommandResult.Success(newState));
        }

        public override string GetDescription()
        {
            return $"Remove card {_cardId} from hand";
        }
    }
}