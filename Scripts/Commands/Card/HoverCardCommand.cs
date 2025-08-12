using Scripts.State;
using Godot;
using System.Linq;

namespace Scripts.Commands.Card
{
    public class StartHoverCommand : GameCommand
    {
        private readonly string _cardId;

        public StartHoverCommand(string cardId) : base()
        {
            _cardId = cardId;
        }

        public override bool CanExecute()
        {
            if (string.IsNullOrEmpty(_cardId)) return false;
            var state = _commandProcessor.CurrentState;
            if (!state.Phase.AllowsCardSelection) return false;
            if (state.Hand.IsLocked) return false;
            var cardInHand = state.Cards.Cards.Any(c => c.CardId == _cardId && c.ContainerType == ContainerType.Hand);
            if (!cardInHand) return false;
            if (state.Cards.HasDragging) return false;
            var currentHover = state.Cards.Cards.FirstOrDefault(c => c.IsHovering);
            if (currentHover != null && currentHover.CardId != _cardId) return false;
            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var current = _commandProcessor.CurrentState;
            GD.Print($"[StartHoverCommand] Hover start for card {_cardId}");
            var newCards = current.Cards.WithCardHovering(_cardId, true);
            var newState = current.WithCards(newCards);
            token.Complete(CommandResult.Success(newState));
        }

        public override string GetDescription() => $"Start hovering card: {_cardId}";
    }

    public class EndHoverCommand : GameCommand
    {
        private readonly string _cardId;

        public EndHoverCommand(string cardId) : base()
        {
            _cardId = cardId;
        }

        public override bool CanExecute()
        {
            if (string.IsNullOrEmpty(_cardId)) return false;
            var state = _commandProcessor.CurrentState;
            var card = state.Cards.Cards.FirstOrDefault(c => c.CardId == _cardId);
            return card?.IsHovering == true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var current = _commandProcessor.CurrentState;
            GD.Print($"[EndHoverCommand] Hover end for card {_cardId}");
            var newCards = current.Cards.WithCardHovering(_cardId, false);
            var newState = current.WithCards(newCards);
            token.Complete(CommandResult.Success(newState));
        }

        public override string GetDescription() => $"End hovering card: {_cardId}";
    }
}