using Godot;
using System;

public partial class Main : Control
{
    private ILogger _logger;

    public override void _EnterTree()
    {
        base._EnterTree();
        ServiceLocator.Initialize();
        ServiceLocator.InitializeNodes(this);
    }

    public override void _Ready()
    {
        try
        {
            base._Ready();
            _logger = ServiceLocator.GetService<ILogger>();
            _logger?.LogInfo("Main scene initialized successfully");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Critical error initializing Main: {ex}");
            throw;
        }
    }
}
