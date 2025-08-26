using Godot;
using Maximagus.Scripts.Enums;
using Maximagus.Resources.Definitions.Actions;
using Scripts.State;

namespace Scripts.Interfaces.Services
{
    public interface ISpellStateService
    {
        SpellState AddModifier(SpellState currentState, ModifierData modifier);

        SpellState UpdateProperty(
            SpellState currentState,
            string key,
            Variant value,
            ContextPropertyOperation operation);

        SpellState UpdateProperty(
            SpellState currentState,
            ContextProperty property,
            float value,
            ContextPropertyOperation operation);

        EncounterState SimulateActionEffectsOnEncounterState(
            EncounterState currentEncounterState,
            ActionResource action,
            ActionExecutionResult actionResult);
    }
}