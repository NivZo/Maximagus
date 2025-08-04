using System;
using Godot;

namespace Scripts.Input
{
    /// <summary>
    /// Handles global mouse input events that aren't card-specific.
    /// This includes background clicks, gestures, and mouse wheel interactions.
    /// </summary>
    public partial class MouseInputHandler : Node
    {
        private InputToCommandMapper _inputMapper;
        private Vector2 _lastMousePosition;
        private bool _isTrackingMouse;

        public override void _Ready()
        {
            // Set to process input for global mouse events
            SetProcessInput(true);
        }

        /// <summary>
        /// Initializes the mouse input handler
        /// </summary>
        /// <param name="inputMapper">The input mapper to send events to</param>
        public void Initialize(InputToCommandMapper inputMapper)
        {
            _inputMapper = inputMapper ?? throw new ArgumentNullException(nameof(inputMapper));
        }

        /// <summary>
        /// Handles global mouse input events
        /// </summary>
        /// <param name="event">The input event</param>
        public override void _Input(InputEvent @event)
        {
            if (_inputMapper == null)
                return;

            var inputData = ProcessMouseEvent(@event);
            if (inputData != null)
            {
                _inputMapper.ProcessInput(inputData);
            }
        }

        /// <summary>
        /// Processes mouse events and converts them to InputEventData
        /// </summary>
        /// <param name="event">The input event</param>
        /// <returns>InputEventData or null if not a relevant mouse event</returns>
        private InputEventData ProcessMouseEvent(InputEvent @event)
        {
            switch (@event)
            {
                case InputEventMouseButton mouseButton:
                    return ProcessMouseButton(mouseButton);
                
                case InputEventMouseMotion mouseMotion:
                    return ProcessMouseMotion(mouseMotion);
                
                default:
                    return null;
            }
        }

        /// <summary>
        /// Processes mouse button events for global actions
        /// </summary>
        /// <param name="mouseButton">The mouse button event</param>
        /// <returns>InputEventData or null if not handled</returns>
        private InputEventData ProcessMouseButton(InputEventMouseButton mouseButton)
        {
            var inputData = new InputEventData(InputType.MouseAction)
            {
                MousePosition = mouseButton.Position,
                IsPressed = mouseButton.Pressed,
                IsShiftPressed = mouseButton.ShiftPressed,
                IsCtrlPressed = mouseButton.CtrlPressed,
                IsAltPressed = mouseButton.AltPressed
            };

            switch (mouseButton.ButtonIndex)
            {
                case MouseButton.Left:
                    inputData.Action = ProcessLeftClick(mouseButton);
                    break;

                case MouseButton.Right:
                    inputData.Action = ProcessRightClick(mouseButton);
                    break;

                case MouseButton.Middle:
                    inputData.Action = ProcessMiddleClick(mouseButton);
                    break;

                case MouseButton.WheelUp:
                case MouseButton.WheelDown:
                    inputData.Action = ProcessMouseWheel(mouseButton);
                    break;

                default:
                    return null;
            }

            return string.IsNullOrEmpty(inputData.Action) ? null : inputData;
        }

        /// <summary>
        /// Processes left mouse button clicks
        /// </summary>
        /// <param name="mouseButton">The mouse button event</param>
        /// <returns>Action string or null if not handled</returns>
        private string ProcessLeftClick(InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed)
            {
                // Check if this is a background click (not on a card)
                if (IsBackgroundClick(mouseButton.Position))
                {
                    return "BackgroundClick";
                }
            }
            else
            {
                // Left mouse release in background
                if (IsBackgroundClick(mouseButton.Position))
                {
                    return "BackgroundRelease";
                }
            }

            return null;
        }

        /// <summary>
        /// Processes right mouse button clicks
        /// </summary>
        /// <param name="mouseButton">The mouse button event</param>
        /// <returns>Action string or null if not handled</returns>
        private string ProcessRightClick(InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed && IsBackgroundClick(mouseButton.Position))
            {
                // Right click in background could show context menu or clear selection
                return "BackgroundRightClick";
            }

            return null;
        }

        /// <summary>
        /// Processes middle mouse button clicks
        /// </summary>
        /// <param name="mouseButton">The mouse button event</param>
        /// <returns>Action string or null if not handled</returns>
        private string ProcessMiddleClick(InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed)
            {
                // Middle click for special actions
                return "MiddleClick";
            }

            return null;
        }

        /// <summary>
        /// Processes mouse wheel events
        /// </summary>
        /// <param name="mouseButton">The mouse button event (wheel)</param>
        /// <returns>Action string for the wheel event</returns>
        private string ProcessMouseWheel(InputEventMouseButton mouseButton)
        {
            switch (mouseButton.ButtonIndex)
            {
                case MouseButton.WheelUp:
                    return "WheelUp";
                
                case MouseButton.WheelDown:
                    return "WheelDown";
                
                default:
                    return null;
            }
        }

        /// <summary>
        /// Processes mouse motion events
        /// </summary>
        /// <param name="mouseMotion">The mouse motion event</param>
        /// <returns>InputEventData or null if not relevant</returns>
        private InputEventData ProcessMouseMotion(InputEventMouseMotion mouseMotion)
        {
            _lastMousePosition = mouseMotion.Position;

            // Only track mouse motion if specifically needed
            if (_isTrackingMouse)
            {
                return new InputEventData(InputType.MouseAction, "MouseMove")
                {
                    MousePosition = mouseMotion.Position
                };
            }

            return null;
        }

        /// <summary>
        /// Determines if a mouse click is on the background (not on a card or UI element)
        /// </summary>
        /// <param name="position">The mouse position</param>
        /// <returns>True if the click is on the background</returns>
        private bool IsBackgroundClick(Vector2 position)
        {
            // This would need to be integrated with the actual UI system
            // For now, we'll assume it's a background click if no specific UI element is hit
            // In a real implementation, this would check against card bounds, UI panels, etc.
            
            // TODO: Implement proper UI hit testing
            // For now, return true to process all clicks as background clicks
            return true;
        }

        /// <summary>
        /// Enables or disables mouse motion tracking
        /// </summary>
        /// <param name="enabled">Whether to track mouse motion</param>
        public void SetMouseTrackingEnabled(bool enabled)
        {
            _isTrackingMouse = enabled;
        }

        /// <summary>
        /// Gets whether mouse motion tracking is enabled
        /// </summary>
        /// <returns>True if mouse tracking is enabled</returns>
        public bool IsMouseTrackingEnabled()
        {
            return _isTrackingMouse;
        }

        /// <summary>
        /// Gets the last recorded mouse position
        /// </summary>
        /// <returns>The last mouse position</returns>
        public Vector2 GetLastMousePosition()
        {
            return _lastMousePosition;
        }

        /// <summary>
        /// Enables or disables mouse input processing
        /// </summary>
        /// <param name="enabled">Whether to enable mouse processing</param>
        public void SetMouseProcessingEnabled(bool enabled)
        {
            SetProcessInput(enabled);
        }

        /// <summary>
        /// Gets whether mouse processing is currently enabled
        /// </summary>
        /// <returns>True if mouse processing is enabled</returns>
        public bool IsMouseProcessingEnabled()
        {
            return IsProcessingInput();
        }

        /// <summary>
        /// Sets a custom hit test function for determining background clicks
        /// This allows integration with the actual UI system
        /// </summary>
        /// <param name="hitTestFunction">Function that returns true if position hits a UI element</param>
        public void SetCustomHitTest(Func<Vector2, bool> hitTestFunction)
        {
            // TODO: Store and use custom hit test function
            // For now, this is a placeholder for future integration
            GD.Print("Custom hit test function registered");
        }
    }
}