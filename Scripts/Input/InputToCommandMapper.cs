using System;
using System.Collections.Generic;
using System.Linq;
using Scripts.Commands;
using Scripts.Commands.Card;
using Scripts.Commands.Hand;
using Scripts.Commands.Game;
using Scripts.State;
using Godot;

namespace Scripts.Input
{
	/// <summary>
	/// Central mapper that converts user inputs into game commands.
	/// This is the core component that bridges user interactions and the command system.
	/// </summary>
	public class InputToCommandMapper
	{
		private readonly IGameCommandProcessor _commandProcessor;
		private readonly Dictionary<string, Func<InputEventData, IGameCommand>> _inputMappings;

		public InputToCommandMapper(IGameCommandProcessor commandProcessor)
		{
			_commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
			_inputMappings = new Dictionary<string, Func<InputEventData, IGameCommand>>();
			InitializeInputMappings();
		}

		/// <summary>
		/// Processes an input event and converts it to a command if applicable
		/// </summary>
		/// <param name="inputData">The input event data</param>
		/// <returns>True if the input was processed and converted to a command</returns>
		public bool ProcessInput(InputEventData inputData)
		{
			if (inputData == null) return false;

			// Process input with new system

			try
			{
				// Try to map the input to a command
				var command = MapInputToCommand(inputData);
				if (command == null) return false;

				// Execute the command through the processor
				var success = _commandProcessor.ExecuteCommand(command);
				
				if (success)
				{
					GD.Print($"[InputMapper] Successfully processed {inputData.Type}: {command.GetDescription()}");
				}

				return success;
			}
			catch (Exception ex)
			{
				GD.Print($"[InputMapper] Error processing input {inputData.Type}: {ex.Message}");
				return false;
			}
		}

		/// <summary>
		/// Maps an input event to a game command
		/// </summary>
		/// <param name="inputData">The input event data</param>
		/// <returns>Game command or null if no mapping exists</returns>
		private IGameCommand MapInputToCommand(InputEventData inputData)
		{
			var mappingKey = GetMappingKey(inputData);
			
			if (_inputMappings.TryGetValue(mappingKey, out var mappingFunc))
			{
				return mappingFunc(inputData);
			}

			// Handle generic mappings that don't fit the dictionary pattern
			return HandleGenericInput(inputData);
		}

		/// <summary>
		/// Gets a mapping key for the input data
		/// </summary>
		private string GetMappingKey(InputEventData inputData)
		{
			return $"{inputData.Type}_{inputData.Action}";
		}

		/// <summary>
		/// Handles inputs that require more complex logic than simple dictionary mapping
		/// </summary>
		private IGameCommand HandleGenericInput(InputEventData inputData)
		{
			return inputData.Type switch
			{
				InputType.CardClick => HandleCardClick(inputData),
				InputType.KeyPress => HandleKeyPress(inputData),
				InputType.MouseAction => HandleMouseAction(inputData),
				InputType.CardDrag => HandleCardDrag(inputData),
				_ => null
			};
		}

		/// <summary>
		/// Handles card click inputs
		/// </summary>
		private IGameCommand HandleCardClick(InputEventData inputData)
		{
			if (string.IsNullOrEmpty(inputData.CardId)) return null;

			var currentState = _commandProcessor.CurrentState;
			
			// Check if card is currently selected
			var isSelected = currentState.Hand.SelectedCardIds.Contains(inputData.CardId);
			
			// Toggle selection based on current state
			return isSelected 
				? new DeselectCardCommand(inputData.CardId)
				: new SelectCardCommand(inputData.CardId);
		}

		/// <summary>
		/// Handles keyboard input
		/// </summary>
		private IGameCommand HandleKeyPress(InputEventData inputData)
		{
			// Use ONLY the new command system - no legacy integration
			return inputData.KeyCode switch
			{
				Key.Enter => new StartGameCommand(),
				Key.Space => new PlayHandCommand(),
				Key.Delete or Key.Backspace => new DiscardHandCommand(),
				_ => null
			};
		}

		/// <summary>
		/// Handles mouse actions (beyond card clicks)
		/// </summary>
		private IGameCommand HandleMouseAction(InputEventData inputData)
		{
			return inputData.Action switch
			{
				"RightClick" when !string.IsNullOrEmpty(inputData.CardId) => 
					new DeselectCardCommand(inputData.CardId),
				"DoubleClick" when !string.IsNullOrEmpty(inputData.CardId) => 
					new SelectCardCommand(inputData.CardId),
				_ => null
			};
		}

		/// <summary>
		/// Handles card drag operations
		/// </summary>
		private IGameCommand HandleCardDrag(InputEventData inputData)
		{
			if (inputData.CardOrder != null && inputData.CardOrder.Count > 0)
			{
				return new ReorderCardsCommand(inputData.CardOrder);
			}
			return null;
		}
		
		/// <summary>
		/// Initializes the input mapping dictionary
		/// </summary>
		private void InitializeInputMappings()
		{
			// Add specific input mappings that don't require complex logic
			// Most mappings are handled in HandleGenericInput for flexibility

			_inputMappings["System_Quit"] = _ => null; // System events don't map to game commands
			_inputMappings["System_Pause"] = _ => null;

			// Add more specific mappings as needed
		}

		/// <summary>
		/// Adds a custom input mapping
		/// </summary>
		/// <param name="inputType">The input type</param>
		/// <param name="action">The action name</param>
		/// <param name="commandFactory">Function to create the command</param>
		public void AddInputMapping(InputType inputType, string action, Func<InputEventData, IGameCommand> commandFactory)
		{
			var key = $"{inputType}_{action}";
			_inputMappings[key] = commandFactory;
		}

		/// <summary>
		/// Removes an input mapping
		/// </summary>
		/// <param name="inputType">The input type</param>
		/// <param name="action">The action name</param>
		public void RemoveInputMapping(InputType inputType, string action)
		{
			var key = $"{inputType}_{action}";
			_inputMappings.Remove(key);
		}

		/// <summary>
		/// Gets the current number of registered input mappings
		/// </summary>
		public int MappingCount => _inputMappings.Count;
	}

	/// <summary>
	/// Data structure representing an input event
	/// </summary>
	public class InputEventData
	{
		public InputType Type { get; set; }
		public string Action { get; set; }
		public string CardId { get; set; }
		public Key KeyCode { get; set; }
		public Vector2 MousePosition { get; set; }
		public List<string> CardOrder { get; set; }
		public bool IsPressed { get; set; }
		public bool IsShiftPressed { get; set; }
		public bool IsCtrlPressed { get; set; }
		public bool IsAltPressed { get; set; }
		public object AdditionalData { get; set; }

		public InputEventData(InputType type, string action = null)
		{
			Type = type;
			Action = action ?? string.Empty;
			CardOrder = new List<string>();
		}
	}

	/// <summary>
	/// Types of input events
	/// </summary>
	public enum InputType
	{
		CardClick,
		CardDrag,
		KeyPress,
		MouseAction,
		System
	}
}
