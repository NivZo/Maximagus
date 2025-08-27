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
			var eventBus = ServiceLocator.GetService<IEventBus>();
			_commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
			_inputMapper = new InputToCommandMapper(_commandProcessor);
			
			_keyboardHandler = new KeyboardInputHandler();
			_mouseHandler = new MouseInputHandler();
			
			AddChild(_keyboardHandler);
			AddChild(_mouseHandler);
			
			_keyboardHandler.Initialize(_inputMapper);
			_mouseHandler.Initialize(_inputMapper);
			
			InitializeGameState();
		}
		catch (Exception ex)
		{
			_logger?.LogError("Failed to initialize new command system", ex);
			throw; // Can't continue without the new system now
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
			var handState = new HandState(maxHandSize: 10, isLocked: false);
			var cardsState = new CardsState();
			
			if (!handState.IsValid())
			{
				_logger.LogError("HandState validation failed");
				return;
			}
			
			var gamePhaseState = new GamePhaseState(
				currentPhase: GamePhase.GameStart,
				turnNumber: 1,
				phaseDescription: "Select cards for your spell"
			);
			
			if (!gamePhaseState.IsValid())
			{
				_logger.LogError("GamePhaseState validation failed");
				return;
			}

			var gameState = GameState.Create(
				cards: cardsState,
				hand: handState,
				player: new PlayerState(),
				enemy: new EnemyState(40, 40),
				phase: gamePhaseState,
				spell: SpellState.CreateInitial(),
				statusEffects: StatusEffectsState.CreateInitial()
			);
			
			_commandProcessor.SetState(gameState);
		}
		catch (Exception ex)
		{
			_logger?.LogError("Failed to initialize GameState", ex);
		}
	}
}
