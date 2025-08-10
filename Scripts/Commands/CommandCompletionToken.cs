using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.State;

namespace Scripts.Commands
{
    public class CommandCompletionToken
    {
        private readonly object _lock = new object();
        private bool _isCompleted = false;
        private CommandResult _result;
        private event Action<CommandResult> _onComplete;

        public void Subscribe(Action<CommandResult> callback)
        {
            lock (_lock)
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
        }

        public void Complete(CommandResult result)
        {
            lock (_lock)
            {
                if (_isCompleted) return;

                _isCompleted = true;
                _result = result;
                var handlers = _onComplete;
                _onComplete = null;
                
                handlers?.Invoke(result);
            }
        }
    }
}