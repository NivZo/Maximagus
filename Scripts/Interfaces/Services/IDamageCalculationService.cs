using System.Collections.Immutable;
using Maximagus.Resources.Definitions.Actions;
using Scripts.State;

namespace Scripts.Interfaces.Services
{
    public interface IDamageCalculationService
    {
        (float finalDamage, ImmutableArray<ModifierData> consumedModifiers, ImmutableArray<ModifierData> remainingModifiers) ApplyDamageModifiers(
            DamageActionResource damageAction,
            EncounterState encounterState);

        float GetRawDamage(DamageActionResource damageAction, EncounterState encounterState);
    }
}