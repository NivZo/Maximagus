using System;
using Godot;

namespace Scripts.Input
{

    public partial class MouseInputHandler : Node
    {
        private InputToCommandMapper _inputMapper;
        private Vector2 _lastMousePosition;
        private bool _isTrackingMouse;
        private ILogger _logger;

        public override void _Ready()
        {
            _logger = ServiceLocator.GetService<ILogger>();
            // Set to process input for global mouse events
            SetProcessInput(true);
        }
        public void Initialize(InputToCommandMapper inputMapper)
        {
            _inputMapper = inputMapper ?? throw new ArgumentNullException(nameof(inputMapper));
        }
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
        private string ProcessRightClick(InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed && IsBackgroundClick(mouseButton.Position))
            {
                // Right click in background could show context menu or clear selection
                return "BackgroundRightClick";
            }

            return null;
        }
        private string ProcessMiddleClick(InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed)
            {
                // Middle click for special actions
                return "MiddleClick";
            }

            return null;
        }
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
        private bool IsBackgroundClick(Vector2 position)
        {
            // This would need to be integrated with the actual UI system
            // For now, we'll assume it's a background click if no specific UI element is hit
            // In a real implementation, this would check against card bounds, UI panels, etc.
            
            // TODO: Implement proper UI hit testing
            // For now, return true to process all clicks as background clicks
            return true;
        }
        public void SetMouseTrackingEnabled(bool enabled)
        {
            _isTrackingMouse = enabled;
        }
        public bool IsMouseTrackingEnabled()
        {
            return _isTrackingMouse;
        }
        public Vector2 GetLastMousePosition()
        {
            return _lastMousePosition;
        }
        public void SetMouseProcessingEnabled(bool enabled)
        {
            SetProcessInput(enabled);
        }
        public bool IsMouseProcessingEnabled()
        {
            return IsProcessingInput();
        }
        public void SetCustomHitTest(Func<Vector2, bool> hitTestFunction)
        {
            // TODO: Store and use custom hit test function
            // For now, this is a placeholder for future integration
            _logger.LogInfo("Custom hit test function registered");
        }
    }
}