using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.State;

namespace Scripts.Commands
{
    public class CommandCompletionToken
    {
        private bool _isCompleted = false;
        private CommandResult _result;
        private event Action<CommandResult> _onComplete;

        public void Subscribe(Action<CommandResult> callback)
        {
            if (_isCompleted)
            {
                callback?.Invoke(_result);
            }
            else
            {
                _onComplete += callback;
            }
        }

        public void Complete(CommandResult result)
        {
            if (_isCompleted) return;

            _isCompleted = true;
            _result = result;
            _onComplete?.Invoke(_result);
            _onComplete = null;
        }
    }
}