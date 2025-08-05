using Godot;
using System;
using Scripts.Commands;
using Scripts.Input;
using Scripts.State;
using Scripts.Validation;
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
            
            // Initialize new command system FIRST
            InitializeNewCommandSystem();
            
            // CRITICAL: Register the initialized GameCommandProcessor with ServiceLocator
            // This must happen AFTER we create it, so cards can access it immediately
            ServiceLocator.RegisterService(_commandProcessor);
            
        }
        catch (Exception ex)
        {
            _logger.LogError($"Critical error initializing Main: {ex}");
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
            
            // CRITICAL: Initialize GameState with real card data from the Hand
            InitializeGameState();
            
            // Notify existing cards that input system is ready (AFTER GameState is set)
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

    private void InitializeGameState()
    {
        try
        {
            _logger.LogInfo("[Main] Initializing GameState with real Hand data");            
            _logger.LogInfo($"[Main] Found {_hand.Cards.Length} real cards, {_hand.SelectedCards.Length} selected");
            
            // Convert real cards to CardState objects
            var cardStates = _hand.Cards.Select(card => new CardState(
                cardId: card.GetInstanceId().ToString(),
                isSelected: card.IsSelected,
                isDragging: false,
                position: 0
            )).ToList();
            
            // Get selected card IDs
            var selectedCardIds = _hand.SelectedCards.Select(card => card.GetInstanceId().ToString()).ToList();
            
            _logger.LogInfo($"[Main] Creating HandState with {cardStates.Count} cards, {selectedCardIds.Count} selected");
            
            // Create HandState with real data
            var handState = new HandState(
                cards: cardStates,
                selectedCardIds: selectedCardIds,
                maxHandSize: 10,
                isLocked: false
            );
            
            // DEBUG: Check if HandState is valid
            if (!handState.IsValid())
            {
                _logger.LogError("[Main] ERROR: HandState is invalid!");
                return;
            }
            
            _logger.LogInfo("[Main] HandState created successfully and is valid");
            
            var gamePhaseState = new GamePhaseState(
                currentPhase: GamePhase.Menu,            // Start in menu phase
                turnNumber: 1,                           // Turn 1
                canPlayerAct: true,                      // Player can act during card selection
                phaseDescription: "Select cards for your spell"  // CRITICAL: Non-empty description
            );
            
            // DEBUG: Check if GamePhaseState is valid
            if (!gamePhaseState.IsValid())
            {
                _logger.LogError("[Main] ERROR: GamePhaseState is invalid!");
                _logger.LogError($"[Main] TurnNumber: {gamePhaseState.TurnNumber}");
                _logger.LogError($"[Main] PhaseDescription: '{gamePhaseState.PhaseDescription}'");
                return;
            }
            
            _logger.LogInfo("[Main] GamePhaseState created successfully and is valid");
            
            // Create complete GameState with real hand data
            var gameState = GameState.Create(
                hand: handState,
                player: new PlayerState(),
                phase: gamePhaseState
            );
            
            // Detailed state validation
            var validator = new StateValidator();
            var validationResult = validator.ValidateState(gameState);
            
            if (!validationResult.IsValid)
            {
                _logger.LogError($"[Main] ERROR: GameState validation failed!");
                foreach (var error in validationResult.Errors)
                {
                    _logger.LogError($"[Main] Validation Error: {error}");
                }
                foreach (var warning in validationResult.Warnings)
                {
                    _logger.LogInfo($"[Main] Validation Warning: {warning}");
                }
                return;
            }
            
            _logger.LogInfo("[Main] GameState validation passed, setting state...");
            
            // Set the GameState in the command processor
            _commandProcessor.SetState(gameState);
            
            _logger.LogInfo($"[Main] GameState initialized successfully with {cardStates.Count} cards, {selectedCardIds.Count} selected");
            foreach (var cardState in cardStates)
            {
                _logger.LogInfo($"[Main] Card ID: {cardState.CardId}, Selected: {cardState.IsSelected}");
            }
            
            _logger?.LogInfo($"GameState initialized with Hand data: {cardStates.Count} cards");
        }
        catch (Exception ex)
        {
            _logger?.LogError("Failed to initialize GameState with Hand data", ex);
            _logger.LogError($"[Main] ERROR initializing GameState: {ex.Message}");
            _logger.LogError($"[Main] Stack trace: {ex.StackTrace}");
        }
    }
}
