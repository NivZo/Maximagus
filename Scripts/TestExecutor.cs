using Godot;
using Tests;

namespace Scripts
{
    /// <summary>
    /// Simple test executor for running spell state tests
    /// </summary>
    public partial class TestExecutor : Node
    {
        private ILogger _logger;

        public override void _Ready()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            // Run tests when the node is ready
            CallDeferred(nameof(RunTests));
        }

        private void RunTests()
        {
            _logger.LogInfo("=== Test Executor Ready ===");
            
            try
            {
                // Run our new EncounterState infrastructure tests
                TestRunner.RunAllEncounterStateTests();
                
                _logger.LogInfo("=== EncounterState Infrastructure Tests Completed Successfully! ===");
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"EncounterState tests failed: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}