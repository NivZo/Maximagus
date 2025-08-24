using System;
using System.Collections.Generic;

namespace Tests.Mocks
{
    /// <summary>
    /// Mock implementation of TimerUtils for testing
    /// </summary>
    public static class MockTimerUtils
    {
        private static readonly List<Action> _pendingCallbacks = new List<Action>();

        public static void ExecuteAfter(Action action, float delay)
        {
            // In tests, we don't actually wait for the delay
            // Instead, we store the callback to be executed manually
            _pendingCallbacks.Add(action);
        }

        public static void ExecutePendingCallbacks()
        {
            var callbacks = new List<Action>(_pendingCallbacks);
            _pendingCallbacks.Clear();
            
            foreach (var callback in callbacks)
            {
                callback?.Invoke();
            }
        }

        public static void Reset()
        {
            _pendingCallbacks.Clear();
        }

        public static int PendingCallbackCount => _pendingCallbacks.Count;
    }
}