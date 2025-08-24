using System;
using Scripts.Commands;

namespace Tests.Mocks
{
    /// <summary>
    /// Mock implementation of CommandCompletionToken for testing
    /// </summary>
    public class MockCommandCompletionToken
    {
        public bool IsCompleted { get; private set; }
        public CommandResult Result { get; private set; }

        public void Complete(CommandResult result)
        {
            if (IsCompleted)
                throw new InvalidOperationException("Token has already been completed");

            IsCompleted = true;
            Result = result;
        }

        public void Reset()
        {
            IsCompleted = false;
            Result = null;
        }
    }
}