using Godot;
using System;
using Scripts.Commands;
using Scripts.Input;

public partial class Main : Control
{
    private ILogger _logger;
    
    // New command system components (replacing legacy systems)
    private GameCommandProcessor _commandProcessor;
    private InputToCommandMapper _inputMapper;
    private KeyboardInputHandler _keyboardHandler;
    private MouseInputHandler _mouseHandler;
    
    // Direct reference to Hand node (no HandManager)
    private Hand _hand;

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
            _logger?.LogInfo("Main scene initialized successfully");

            // Get direct reference to Hand node (no legacy HandManager)
            _hand = GetNode<Hand>("Hand");
            
            // Initialize new command system (replaces legacy systems)
            InitializeNewCommandSystem();
            
            // Connect Hand as observer to GameState changes
            _hand.SetGameCommandProcessor(_commandProcessor);
            
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Critical error initializing Main: {ex}");
            throw;
        }
    }

    private void InitializeNewCommandSystem()
    {
        try
        {
            // Get existing event bus from service locator
            var eventBus = ServiceLocator.GetService<IEventBus>();
            
            // Initialize command processor with initial game state
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
            
            // Notify existing cards that input system is ready
            NotifyCardsInputSystemReady();
            
            _logger?.LogInfo("New command system initialized successfully");
            _logger?.LogInfo("LEGACY SYSTEMS DISABLED - Using only new command architecture");
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to initialize new command system", ex);
            throw; // Can't continue without the new system now
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

    /// <summary>
    /// Notifies all existing cards that the input system is now available
    /// </summary>
    private void NotifyCardsInputSystemReady()
    {
        try
        {
            // Find all Card nodes in the scene tree
            var cards = new Godot.Collections.Array();
            FindCardsRecursive(this, cards);

            foreach (Node node in cards)
            {
                if (node is Card card)
                {
                    card.NotifyInputSystemReady(_inputMapper);
                }
            }

            _logger?.LogInfo($"Notified {cards.Count} cards that input system is ready");
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to notify cards about input system", ex);
        }
    }

    private void FindCardsRecursive(Node node, Godot.Collections.Array cards)
    {
        if (node is Card card)
        {
            cards.Add(card);
        }

        foreach (Node child in node.GetChildren())
        {
            FindCardsRecursive(child, cards);
        }
    }
}
