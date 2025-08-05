using Godot;
using System;
using Scripts.Commands;
using Scripts.Input;
using Scripts.State;
using System.Linq;

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

            // Get direct reference to Hand node
            _hand = GetNode<Hand>("Hand");
            
            // Setup HandManager with Hand node for proper access
            var handManager = ServiceLocator.GetService<IHandManager>();
            handManager.SetupHandNode(_hand);
            
            // Initialize new command system
            InitializeNewCommandSystem();
            
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

    /// <summary>
    /// Initializes the GameState with actual card data from the real Hand
    /// </summary>
    private void InitializeGameStateWithRealHandData()
    {
        try
        {
            Console.WriteLine("[Main] Initializing GameState with real Hand data");
            
            // Get actual cards from the real Hand
            var realCards = _hand.Cards;
            var realSelectedCards = _hand.SelectedCards;
            
            // Convert real cards to CardState objects
            var cardStates = realCards.Select(card => new CardState(
                cardId: card.GetInstanceId().ToString(),
                isSelected: card.IsSelected,
                isDragging: false,
                position: 0
            )).ToList();
            
            // Get selected card IDs
            var selectedCardIds = realSelectedCards.Select(card => card.GetInstanceId().ToString()).ToList();
            
            // Create HandState with real data
            var handState = new HandState(
                cards: cardStates,
                selectedCardIds: selectedCardIds,
                maxHandSize: 10,
                isLocked: false
            );
            
            // Create complete GameState with real hand data
            var gameState = GameState.Create(
                hand: handState,
                player: new PlayerState(),
                phase: new GamePhaseState()
            );
            
            // Set the GameState in the command processor
            _commandProcessor.SetState(gameState, clearHistory: true);
            
            Console.WriteLine($"[Main] GameState initialized with {cardStates.Count} cards, {selectedCardIds.Count} selected");
            _logger?.LogInfo($"GameState initialized with real Hand data: {cardStates.Count} cards");
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to initialize GameState with real Hand data", ex);
            Console.WriteLine($"[Main] ERROR initializing GameState: {ex.Message}");
        }
    }
}
