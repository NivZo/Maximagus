using System;
using Scripts.State;
using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;

namespace Scripts.Commands.Spell
{

    public class AddSpellModifierCommand : GameCommand
    {
        private readonly ModifierType _modifierType;
        private readonly DamageType _element;
        private readonly float _value;
        private readonly bool _isConsumedOnUse;
        private readonly SpellModifierCondition[] _conditions;

        public AddSpellModifierCommand(
            ModifierType modifierType,
            DamageType element,
            float value,
            bool isConsumedOnUse = true,
            SpellModifierCondition[] conditions = null) : base(false)
        {
            _modifierType = modifierType;
            _element = element;
            _value = value;
            _isConsumedOnUse = isConsumedOnUse;
            _conditions = conditions ?? Array.Empty<SpellModifierCondition>();
        }

        public override bool CanExecute()
        {
            var currentState = _commandProcessor.CurrentState;
            if (currentState == null) return false;
            
            // Must have an active spell to add modifiers
            if (!currentState.Spell.IsActive)
            {
                _logger.LogWarning("[AddSpellModifierCommand] Cannot add modifier - no active spell");
                return false;
            }

            return true;
        }

        public override void Execute(CommandCompletionToken token)
        {
            var currentState = _commandProcessor.CurrentState;
            
            _logger.LogInfo($"[AddSpellModifierCommand] Adding modifier: {_modifierType} {_value} {_element} (Consumed: {_isConsumedOnUse})");
            
            try
            {
                // Create modifier data
                var modifier = new ModifierData(
                    type: _modifierType,
                    element: _element,
                    value: _value,
                    isConsumedOnUse: _isConsumedOnUse,
                    conditions: System.Collections.Immutable.ImmutableArray.Create(_conditions));

                // Use SpellLogicManager to add the modifier
                var newSpellState = SpellLogicManager.AddModifier(currentState.Spell, modifier);
                var newState = currentState.WithSpell(newSpellState);
                
                _logger.LogInfo($"[AddSpellModifierCommand] Modifier added successfully. Total modifiers: {newSpellState.ActiveModifiers.Length}");
                
                token.Complete(CommandResult.Success(newState));
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AddSpellModifierCommand] Error adding modifier: {ex.Message}");
                token.Complete(CommandResult.Failure($"Failed to add spell modifier: {ex.Message}"));
            }
        }

        public override string GetDescription()
        {
            return $"Add spell modifier: {_modifierType} {_value} {_element}";
        }
    }
}