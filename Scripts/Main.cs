using Godot;
using System;
using Scripts.Commands;
using Scripts.Input;
using Scripts.State;

public partial class Main : Control
{
	private ILogger _logger;
	
	// New command system components (replacing legacy systems)
	private IGameCommandProcessor _commandProcessor;
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
			_logger?.LogInfo("Main scene initialized successfully");

			InitializeCommandSystem();
		}
		catch (Exception ex)
		{
			_logger.LogError($"Critical error initializing Main: {ex}");
			throw;
		}
	}

	private void InitializeCommandSystem()
	{
		try
		{
			// Get existing event bus from service locator
			var eventBus = ServiceLocator.GetService<IEventBus>();
			
			// Initialize command processor with initial game state
			_commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();

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

			// Create HandState with real data
			var handState = new HandState(
				cards: [],
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
			
			_commandProcessor.SetState(gameState);			
			_logger?.LogInfo($"GameState initialized");
		}
		catch (Exception ex)
		{
			_logger?.LogError("Failed to initialize GameState with Hand data", ex);
			_logger.LogError($"[Main] ERROR initializing GameState: {ex}");
			_logger.LogError($"[Main] Stack trace: {ex.StackTrace}");
		}
	}
}
