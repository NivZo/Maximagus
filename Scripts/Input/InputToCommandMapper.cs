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
	public class InputToCommandMapper
	{
		private readonly IGameCommandProcessor _commandProcessor;
		private readonly Dictionary<string, Func<InputEventData, GameCommand>> _inputMappings;

		public InputToCommandMapper(IGameCommandProcessor commandProcessor)
		{
			_commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
			_inputMappings = new Dictionary<string, Func<InputEventData, GameCommand>>();
			InitializeInputMappings();
		}

		public bool ProcessInput(InputEventData inputData)
		{
			if (inputData == null) return false;

			try
			{
				var command = MapInputToCommand(inputData);
				if (command == null) return false;

				return _commandProcessor.ExecuteCommand(command);
			}
			catch (Exception ex)
			{
				GD.Print($"Error processing input: {ex.Message}");
				return false;
			}
		}

		private GameCommand MapInputToCommand(InputEventData inputData)
		{
			var mappingKey = GetMappingKey(inputData);
			
			if (_inputMappings.TryGetValue(mappingKey, out var mappingFunc))
			{
				return mappingFunc(inputData);
			}

			return HandleGenericInput(inputData);
		}

		private string GetMappingKey(InputEventData inputData)
		{
			return $"{inputData.Type}_{inputData.Action}";
		}

		private GameCommand HandleGenericInput(InputEventData inputData)
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

		private GameCommand HandleCardClick(InputEventData inputData)
		{
			if (string.IsNullOrEmpty(inputData.CardId)) return null;

			var currentState = _commandProcessor.CurrentState;
			var isSelected = currentState.Cards.SelectedInHand.Select(card => card.CardId).Contains(inputData.CardId);
			
			return isSelected
			    ? new DeselectCardCommand(inputData.CardId)
			    : new SelectCardCommand(inputData.CardId);
		}

		private GameCommand HandleKeyPress(InputEventData inputData)
		{
			return inputData.KeyCode switch
			{
				Key.Enter => new StartGameCommand(),
				Key.Space => new PlayHandCommand(),
				Key.Delete or Key.Backspace => new DiscardHandCommand(),
				_ => null
			};
		}

		private GameCommand HandleMouseAction(InputEventData inputData)
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

		private GameCommand HandleCardDrag(InputEventData inputData)
		{
			if (inputData.CardOrder != null && inputData.CardOrder.Count > 0)
			{
				return new ReorderCardsCommand(inputData.CardOrder);
			}
			return null;
		}
		
		private void InitializeInputMappings()
		{
			_inputMappings["System_Quit"] = _ => null;
			_inputMappings["System_Pause"] = _ => null;
		}

		public void AddInputMapping(InputType inputType, string action, Func<InputEventData, GameCommand> commandFactory)
		{
			var key = $"{inputType}_{action}";
			_inputMappings[key] = commandFactory;
		}

		public void RemoveInputMapping(InputType inputType, string action)
		{
			var key = $"{inputType}_{action}";
			_inputMappings.Remove(key);
		}

		public int MappingCount => _inputMappings.Count;
	}

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

	public enum InputType
	{
		CardClick,
		CardDrag,
		KeyPress,
		MouseAction,
		System
	}
}
