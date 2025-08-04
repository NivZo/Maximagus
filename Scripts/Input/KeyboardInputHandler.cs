using System;
using Godot;

namespace Scripts.Input
{
    /// <summary>
    /// Handles global keyboard input events and converts them to game commands.
    /// This class manages keyboard shortcuts and global key bindings.
    /// </summary>
    public partial class KeyboardInputHandler : Node
    {
        private InputToCommandMapper _inputMapper;

        public override void _Ready()
        {
            // Set to process unhandled input to catch global keyboard events
            SetProcessUnhandledInput(true);
        }

        /// <summary>
        /// Initializes the keyboard input handler
        /// </summary>
        /// <param name="inputMapper">The input mapper to send events to</param>
        public void Initialize(InputToCommandMapper inputMapper)
        {
            _inputMapper = inputMapper ?? throw new ArgumentNullException(nameof(inputMapper));
        }

        /// <summary>
        /// Handles unhandled input events for global keyboard shortcuts
        /// </summary>
        /// <param name="event">The input event</param>
        public override void _UnhandledInput(InputEvent @event)
        {
            if (_inputMapper == null)
                return;

            var inputData = ProcessKeyboardEvent(@event);
            if (inputData != null)
            {
                var processed = _inputMapper.ProcessInput(inputData);
                if (processed)
                {
                    // Mark the event as handled to prevent further processing
                    GetViewport().SetInputAsHandled();
                }
            }
        }

        /// <summary>
        /// Processes keyboard events and converts them to InputEventData
        /// </summary>
        /// <param name="event">The input event</param>
        /// <returns>InputEventData or null if not a relevant keyboard event</returns>
        private InputEventData ProcessKeyboardEvent(InputEvent @event)
        {
            if (@event is not InputEventKey keyEvent || !keyEvent.Pressed)
                return null;

            var inputData = new InputEventData(InputType.KeyPress)
            {
                KeyCode = keyEvent.Keycode,
                Action = GetKeyAction(keyEvent),
                IsShiftPressed = keyEvent.ShiftPressed,
                IsCtrlPressed = keyEvent.CtrlPressed,
                IsAltPressed = keyEvent.AltPressed
            };

            // Only process global keyboard shortcuts
            if (IsGlobalShortcut(keyEvent))
            {
                return inputData;
            }

            return null;
        }

        /// <summary>
        /// Determines the action string for a key event
        /// </summary>
        /// <param name="keyEvent">The key event</param>
        /// <returns>Action string describing the key combination</returns>
        private string GetKeyAction(InputEventKey keyEvent)
        {
            var action = keyEvent.AsText();

            // Handle special key combinations
            if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.Z)
                return "Undo";
            
            if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.Y)
                return "Redo";
            
            if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.A)
                return "SelectAll";
            
            if (keyEvent.CtrlPressed && keyEvent.Keycode == Key.D)
                return "DeselectAll";

            return action;
        }

        /// <summary>
        /// Checks if a key event represents a global shortcut
        /// </summary>
        /// <param name="keyEvent">The key event to check</param>
        /// <returns>True if this is a global shortcut that should be processed</returns>
        private bool IsGlobalShortcut(InputEventKey keyEvent)
        {
            // Handle individual keys that should be processed globally
            switch (keyEvent.Keycode)
            {
                case Key.Enter:
                case Key.Space:
                    return true; // Play hand
                
                case Key.Delete:
                case Key.Backspace:
                    return true; // Discard hand
                
                case Key.Escape:
                    return true; // Clear selection
                
                case Key.F1:
                case Key.F2:
                case Key.F3:
                case Key.F4:
                    return true; // Debug/help functions
                
                default:
                    break;
            }

            // Handle Ctrl combinations
            if (keyEvent.CtrlPressed)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.Z: // Undo
                    case Key.Y: // Redo
                    case Key.A: // Select all
                    case Key.D: // Deselect all
                        return true;
                    
                    default:
                        break;
                }
            }

            // Handle Alt combinations for advanced features
            if (keyEvent.AltPressed)
            {
                switch (keyEvent.Keycode)
                {
                    case Key.Key1:
                    case Key.Key2:
                    case Key.Key3:
                    case Key.Key4:
                    case Key.Key5:
                        return true; // Quick card selection
                    
                    default:
                        break;
                }
            }

            return false;
        }

        /// <summary>
        /// Enables or disables keyboard input processing
        /// </summary>
        /// <param name="enabled">Whether to enable keyboard processing</param>
        public void SetKeyboardProcessingEnabled(bool enabled)
        {
            SetProcessUnhandledInput(enabled);
        }

        /// <summary>
        /// Gets whether keyboard processing is currently enabled
        /// </summary>
        /// <returns>True if keyboard processing is enabled</returns>
        public bool IsKeyboardProcessingEnabled()
        {
            return IsProcessingUnhandledInput();
        }

        /// <summary>
        /// Adds a custom keyboard shortcut
        /// This could be extended to support runtime shortcut configuration
        /// </summary>
        /// <param name="keyCode">The key code</param>
        /// <param name="modifiers">Modifier keys required</param>
        /// <param name="action">Action string for the shortcut</param>
        public void AddCustomShortcut(Key keyCode, KeyModifierMask modifiers, string action)
        {
            // TODO: Implement custom shortcut storage and processing
            // For now, shortcuts are hardcoded in IsGlobalShortcut
            GD.Print($"Custom shortcut registered: {keyCode} + {modifiers} -> {action}");
        }

        /// <summary>
        /// Gets help text for available keyboard shortcuts
        /// </summary>
        /// <returns>String describing available shortcuts</returns>
        public string GetShortcutHelp()
        {
            return "Keyboard Shortcuts:\n" +
                   "Enter/Space - Play selected cards\n" +
                   "Delete/Backspace - Discard selected cards\n" +
                   "Escape - Clear selection\n" +
                   "Ctrl+Z - Undo last action\n" +
                   "Ctrl+Y - Redo last action\n" +
                   "Ctrl+A - Select all cards\n" +
                   "Ctrl+D - Deselect all cards\n" +
                   "Alt+1-5 - Quick select card by position\n" +
                   "F1 - Show help";
        }
    }
}