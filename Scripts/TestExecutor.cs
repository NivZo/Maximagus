using Godot;
using Tests;

namespace Scripts
{
    /// <summary>
    /// Simple test executor for running spell state tests
    /// </summary>
    public partial class TestExecutor : Node
    {
        public override void _Ready()
        {
            // Run tests when the node is ready
            CallDeferred(nameof(RunTests));
        }

        private void RunTests()
        {
            GD.Print("=== Test Executor Ready ===");
            
            try
            {
                // Run our new EncounterState infrastructure tests
                TestRunner.RunAllEncounterStateTests();
                
                GD.Print("=== EncounterState Infrastructure Tests Completed Successfully! ===");
            }
            catch (System.Exception ex)
            {
                GD.PrintErr($"EncounterState tests failed: {ex.Message}");
                GD.PrintErr($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}