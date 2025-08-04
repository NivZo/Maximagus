using System;
using Godot;

namespace Scripts.Input
{
    /// <summary>
    /// Handles card-specific input events and converts them to InputEventData.
    /// This class is responsible for capturing mouse and keyboard interactions with cards.
    /// </summary>
    public partial class CardInputHandler : Node
    {
        private InputToCommandMapper _inputMapper;
        private string _cardId;
        private bool _isDragging;
        private Vector2 _dragStartPosition;
        private bool _isHovering;

        public override void _Ready()
        {
            // This will be set externally when the handler is attached to a card
        }

        /// <summary>
        /// Initializes the card input handler with the necessary dependencies
        /// </summary>
        /// <param name="cardId">The ID of the card this handler manages</param>
        /// <param name="inputMapper">The input mapper to send events to</param>
        public void Initialize(string cardId, InputToCommandMapper inputMapper)
        {
            _cardId = cardId ?? throw new ArgumentNullException(nameof(cardId));
            _inputMapper = inputMapper ?? throw new ArgumentNullException(nameof(inputMapper));
        }

        /// <summary>
        /// Handles input events for the card
        /// </summary>
        /// <param name="event">The input event</param>
        public override void _Input(InputEvent @event)
        {
            if (string.IsNullOrEmpty(_cardId) || _inputMapper == null)
                return;

            var inputData = ProcessInputEvent(@event);
            if (inputData != null)
            {
                // Try to process through the input mapper
                _inputMapper.ProcessInput(inputData);
            }
        }

        /// <summary>
        /// Processes a Godot input event and converts it to InputEventData
        /// </summary>
        /// <param name="event">The Godot input event</param>
        /// <returns>InputEventData or null if event is not relevant</returns>
        private InputEventData ProcessInputEvent(InputEvent @event)
        {
            switch (@event)
            {
                case InputEventMouseButton mouseButton:
                    return ProcessMouseButton(mouseButton);
                
                case InputEventMouseMotion mouseMotion:
                    return ProcessMouseMotion(mouseMotion);
                
                // Cards should NOT process keyboard events at all!
                // All keyboard input is handled by KeyboardInputHandler
                
                default:
                    return null;
            }
        }

        /// <summary>
        /// Processes mouse button events
        /// </summary>
        private InputEventData ProcessMouseButton(InputEventMouseButton mouseButton)
        {
            if (!_isHovering) return null; // Only process if mouse is over this card

            var inputData = new InputEventData(InputType.CardClick)
            {
                CardId = _cardId,
                MousePosition = mouseButton.Position,
                IsPressed = mouseButton.Pressed,
                IsShiftPressed = mouseButton.ShiftPressed,
                IsCtrlPressed = mouseButton.CtrlPressed,
                IsAltPressed = mouseButton.AltPressed
            };

            switch (mouseButton.ButtonIndex)
            {
                case MouseButton.Left:
                    if (mouseButton.Pressed)
                    {
                        inputData.Action = mouseButton.DoubleClick ? "DoubleClick" : "LeftClick";
                        _dragStartPosition = mouseButton.Position;
                    }
                    else
                    {
                        inputData.Action = "LeftRelease";
                        if (_isDragging)
                        {
                            _isDragging = false;
                            inputData.Type = InputType.CardDrag;
                            inputData.Action = "DragEnd";
                        }
                    }
                    break;

                case MouseButton.Right:
                    inputData.Action = mouseButton.Pressed ? "RightClick" : "RightRelease";
                    break;

                case MouseButton.Middle:
                    inputData.Action = mouseButton.Pressed ? "MiddleClick" : "MiddleRelease";
                    break;

                default:
                    return null;
            }

            return inputData;
        }

        /// <summary>
        /// Processes mouse motion events
        /// </summary>
        private InputEventData ProcessMouseMotion(InputEventMouseMotion mouseMotion)
        {
            // Handle drag detection
            if (!_isDragging && Godot.Input.IsMouseButtonPressed(MouseButton.Left) && _isHovering)
            {
                var dragDistance = mouseMotion.Position.DistanceTo(_dragStartPosition);
                const float dragThreshold = 10.0f;

                if (dragDistance > dragThreshold)
                {
                    _isDragging = true;
                    return new InputEventData(InputType.CardDrag, "DragStart")
                    {
                        CardId = _cardId,
                        MousePosition = mouseMotion.Position
                    };
                }
            }

            // Handle ongoing drag
            if (_isDragging)
            {
                return new InputEventData(InputType.CardDrag, "DragMove")
                {
                    CardId = _cardId,
                    MousePosition = mouseMotion.Position
                };
            }

            return null;
        }

        // Cards should NOT process keyboard events at all!
        // All keyboard input is handled by KeyboardInputHandler
        // Cards only handle mouse interactions: clicks, drags, hover

        /// <summary>
        /// Called when mouse enters the card area
        /// </summary>
        public void OnMouseEntered()
        {
            _isHovering = true;
            
            var inputData = new InputEventData(InputType.MouseAction, "MouseEnter")
            {
                CardId = _cardId
            };
            
            _inputMapper?.ProcessInput(inputData);
        }

        /// <summary>
        /// Called when mouse exits the card area
        /// </summary>
        public void OnMouseExited()
        {
            _isHovering = false;
            _isDragging = false;
            
            var inputData = new InputEventData(InputType.MouseAction, "MouseExit")
            {
                CardId = _cardId
            };
            
            _inputMapper?.ProcessInput(inputData);
        }

        /// <summary>
        /// Sets the card ID for this handler
        /// </summary>
        /// <param name="cardId">The new card ID</param>
        public void SetCardId(string cardId)
        {
            _cardId = cardId;
        }

        /// <summary>
        /// Gets the current card ID
        /// </summary>
        public string GetCardId()
        {
            return _cardId;
        }

        /// <summary>
        /// Gets whether the card is currently being dragged
        /// </summary>
        public bool IsDragging()
        {
            return _isDragging;
        }

        /// <summary>
        /// Gets whether the mouse is currently hovering over the card
        /// </summary>
        public bool IsHovering()
        {
            return _isHovering;
        }

        /// <summary>
        /// Forces the handler to stop dragging (useful for interrupting drags)
        /// </summary>
        public void StopDragging()
        {
            if (_isDragging)
            {
                _isDragging = false;
                
                var inputData = new InputEventData(InputType.CardDrag, "DragCancel")
                {
                    CardId = _cardId
                };
                
                _inputMapper?.ProcessInput(inputData);
            }
        }
    }
}