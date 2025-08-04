using Godot;
using System;
using Scripts.Commands;
using Scripts.Input;

public partial class Main : Control
{
    private ILogger _logger;
    private IGameStateManager _gameStateManager;
    private IHandManager _handManager;
    
    // New input system components
    private GameCommandProcessor _commandProcessor;
    private InputToCommandMapper _inputMapper;
    private KeyboardInputHandler _keyboardHandler;
    private MouseInputHandler _mouseHandler;

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
            
            // Initialize new input system
            InitializeNewInputSystem();
            
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

    private void InitializeNewInputSystem()
    {
        try
        {
            // Get existing event bus from service locator
            var eventBus = ServiceLocator.GetService<IEventBus>();
            
            // Initialize command processor
            _commandProcessor = new GameCommandProcessor(eventBus);
            
            // Initialize input mapper
            _inputMapper = new InputToCommandMapper(_commandProcessor);
            
            // Create and add global input handlers
            _keyboardHandler = new KeyboardInputHandler();
            _mouseHandler = new MouseInputHandler();
            
            AddChild(_keyboardHandler);
            AddChild(_mouseHandler);
            
            // Initialize handlers
            _keyboardHandler.Initialize(_inputMapper);
            _mouseHandler.Initialize(_inputMapper);
            
            // Store references for other components to use
            // Note: ServiceLocator doesn't support direct instance registration,
            // so we'll provide a way for Card components to access these
            
            _logger?.LogInfo("New input system initialized successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to initialize new input system", ex);
            // Don't throw - allow game to continue with legacy system
        }
    }

    /// <summary>
    /// Gets the input mapper for card components to use
    /// </summary>
    public InputToCommandMapper GetInputMapper()
    {
        return _inputMapper;
    }

    /// <summary>
    /// Gets the command processor for components that need direct access
    /// </summary>
    public GameCommandProcessor GetCommandProcessor()
    {
        return _commandProcessor;
    }
}
