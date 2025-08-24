using System;
using Scripts.State;
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Spell
{
    /// <summary>
    /// Command to update spell context properties using various operations (Add, Multiply, Set).
    /// Supports both string-based and enum-based property keys.
    /// </summary>
    public class UpdateSpellPropertyCommand : GameCommand
    {
        private readonly string _propertyKey;
        private readonly Variant _value;
        private readonly ContextPropertyOperation _operation;

        public UpdateSpellPropertyCommand(
            string propertyKey, 
            Variant value, 
            ContextPropertyOperation operation) : base(false)
        {
            _propertyKey = propertyKey ?? throw new ArgumentNullException(nameof(propertyKey));
            _value = value;
            _operation = operation;
        }

        public UpdateSpellPropertyCommand(
            ContextProperty property, 
            float value, 
            ContextPropertyOperation operation) : base(false)
        {
            _propertyKey = property.ToString();
            _value = Variant.From(value);
            _operation = operation;
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState == null) return false;
            
            // Must have an active spell to update properties
            if (!currentState.Spell.IsActive)
            {
                _logger.LogWarning("[UpdateSpellPropertyCommand] Cannot update property - no active spell");
                return false;
            }

            if (string.IsNullOrEmpty(_propertyKey))
            {
                _logger.LogWarning("[UpdateSpellPropertyCommand] Cannot update property - key is null or empty");
                return false;
            }

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            
            GD.Print($"[UpdateSpellPropertyCommand] Updating property {_propertyKey} with operation {_operation} and value {_value}");
            
            try
            {
                // Use SpellLogicManager to update the property
                var newSpellState = SpellLogicManager.UpdateProperty(
                    currentState.Spell, 
                    _propertyKey, 
                    _value, 
                    _operation);

                var newState = currentState.WithSpell(newSpellState);
                
                var newValue = newSpellState.GetProperty(_propertyKey, 0f);
                GD.Print($"[UpdateSpellPropertyCommand] Property {_propertyKey} updated to {newValue}");
                
                token.Complete(CommandResult.Success(newState));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[UpdateSpellPropertyCommand] Error updating property {_propertyKey}: {ex.Message}");
                token.Complete(CommandResult.Failure($"Failed to update spell property: {ex.Message}"));
            }
        }

        public override string GetDescription()
        {
            return $"Update spell property {_propertyKey} {_operation} {_value}";
        }
    }
}