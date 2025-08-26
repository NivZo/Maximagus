using System;
using System.Collections.Immutable;
using Godot;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;
using Scripts.Interfaces.Services;
using Scripts.State;
using Scripts.Utils;

namespace Maximagus.Scripts.Services
{
    public class SpellStateService : ISpellStateService
    {
        private readonly ILogger _logger;

        public SpellStateService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public SpellState AddModifier(SpellState currentState, ModifierData modifier)
        {
            if (currentState == null)
                throw new ArgumentNullException(nameof(currentState));
            if (modifier == null)
                throw new ArgumentNullException(nameof(modifier));

            _logger.LogInfo($"[Spell Casting Refactor] AddModifier: Adding {modifier.Type} {modifier.Value} {modifier.Element} modifier");
            return currentState.WithAddedModifier(modifier);
        }

        public SpellState UpdateProperty(
            SpellState currentState,
            string key,
            Variant value,
            ContextPropertyOperation operation)
        {
            if (currentState == null)
                throw new ArgumentNullException(nameof(currentState));
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            var currentValue = currentState.GetProperty(key, 0f);
            var floatValue = value.AsSingle();

            var newValue = operation switch
            {
                ContextPropertyOperation.Add => currentValue + floatValue,
                ContextPropertyOperation.Multiply => currentValue * floatValue,
                ContextPropertyOperation.Set => floatValue,
                _ => currentValue
            };

            _logger.LogInfo($"[Spell Casting Refactor] UpdateProperty: {key} {operation} {floatValue} ({currentValue} -> {newValue})");
            return currentState.WithProperty(key, Variant.From(newValue));
        }

        public SpellState UpdateProperty(
            SpellState currentState,
            ContextProperty property,
            float value,
            ContextPropertyOperation operation)
        {
            return UpdateProperty(currentState, property.ToString(), Variant.From(value), operation);
        }

        public EncounterState SimulateActionEffectsOnEncounterState(
            EncounterState currentEncounterState,
            ActionResource action,
            ActionExecutionResult actionResult)
        {
            switch (action)
            {
                case DamageActionResource damageAction:
                    // Use the precalculated remaining modifiers from actionResult - no need to recalculate!
                    var remainingModifiers = actionResult.RemainingModifiers;
                    
                    // Update spell state with remaining modifiers and context properties
                    var currentSpellState = currentEncounterState.Spell;
                    var newTotalDamage = currentSpellState.TotalDamageDealt + actionResult.FinalDamage;
                    var newActionIndex = currentEncounterState.ActionIndex + 1;
                    var newSpellState = currentSpellState
                        .WithModifiers(remainingModifiers)
                        .WithTotalDamage(newTotalDamage)
                        .WithActionIndex(newActionIndex);
                    
                    // Update context properties
                    if (actionResult.FinalDamage > 0)
                    {
                        var damageDealtProperty = damageAction.DamageType switch
                        {
                            DamageType.Fire => ContextProperty.FireDamageDealt,
                            DamageType.Frost => ContextProperty.FrostDamageDealt,
                            DamageType.PerChill => ContextProperty.FrostDamageDealt,
                            _ => throw new ArgumentException($"No context property implemented for damage type {damageAction.DamageType}")
                        };
                        newSpellState = UpdateProperty(newSpellState, damageDealtProperty, actionResult.FinalDamage, ContextPropertyOperation.Add);
                    }
                    
                    // Update both spell state and action index in encounter state
                    return currentEncounterState.WithSpell(newSpellState).WithActionIndex(newActionIndex);
                    
                case ModifierActionResource modifierAction:
                    // Use the precalculated remaining modifiers from actionResult
                    var remainingModifiersForModifier = actionResult.RemainingModifiers;
                    var newActionIndexForModifier = currentEncounterState.ActionIndex + 1;
                    
                    // Create updated spell state with the precalculated modifiers
                    var currentSpellStateForModifier = currentEncounterState.Spell;
                    var updatedSpellStateWithModifier = currentSpellStateForModifier
                        .WithModifiers(remainingModifiersForModifier)
                        .WithActionIndex(newActionIndexForModifier);
                    
                    return currentEncounterState.WithSpell(updatedSpellStateWithModifier).WithActionIndex(newActionIndexForModifier);
                    
                case StatusEffectActionResource statusEffectAction:
                    var newStatusEffectsState = StatusEffectLogicManager.ApplyStatusEffectToEncounter(
                        currentEncounterState,
                        statusEffectAction.StatusEffect,
                        statusEffectAction.Stacks,
                        statusEffectAction.ActionType);
                    var newActionIndexForStatus = currentEncounterState.ActionIndex + 1;
                    var updatedSpellStateWithStatus = newStatusEffectsState.Spell.WithActionIndex(newActionIndexForStatus);
                    return newStatusEffectsState.WithSpell(updatedSpellStateWithStatus).WithActionIndex(newActionIndexForStatus);
                    
                default:
                    var newActionIndexDefault = currentEncounterState.ActionIndex + 1;
                    var updatedSpellStateDefault = currentEncounterState.Spell.WithActionIndex(newActionIndexDefault);
                    return currentEncounterState.WithSpell(updatedSpellStateDefault).WithActionIndex(newActionIndexDefault);
            }
        }
    }
}