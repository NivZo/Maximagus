using Godot;
using System;

public partial class Main : Control
{
    private ILogger _logger;
    private IGameStateManager _gameStateManager;
    private IHandManager _handManager;

    public override void _EnterTree()
    {
        base._EnterTree();
        ServiceLocator.Initialize(this);
    }

    public override void _Ready()
    {
        try
        {
            base._Ready();
            _logger = ServiceLocator.GetService<ILogger>();
            _gameStateManager = ServiceLocator.GetService<IGameStateManager>();
            _handManager = ServiceLocator.GetService<IHandManager>();
            _logger?.LogInfo("Main scene initialized successfully");

            _handManager.SetupHandNode(GetNode<Hand>("Hand"));
            _gameStateManager.StartGame();
            
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Critical error initializing Main: {ex}");
            throw;
        }
    }
}
