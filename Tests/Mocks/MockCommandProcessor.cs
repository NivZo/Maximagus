using Scripts.Commands;
using Scripts.State;

namespace Tests.Mocks
{
    /// <summary>
    /// Mock implementation of IGameCommandProcessor for testing
    /// </summary>
    public static class MockCommandProcessor
    {
        private static IGameStateData _currentState;

        public static IGameStateData CurrentState => _currentState;

        public static void SetCurrentState(IGameStateData state)
        {
            _currentState = state;
        }

        public static void Reset()
        {
            _currentState = null;
        }
    }
}