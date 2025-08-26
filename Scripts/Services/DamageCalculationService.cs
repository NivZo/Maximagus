using System;
using System.Collections.Immutable;
using Maximagus.Resources.Definitions.Actions;
using Maximagus.Scripts.Enums;
using Maximagus.Scripts.Managers;
using Scripts.Interfaces.Services;
using Scripts.State;
using Scripts.Utils;

namespace Maximagus.Scripts.Services
{
    public class DamageCalculationService : IDamageCalculationService
    {
        private readonly ILogger _logger;

        public DamageCalculationService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public (float finalDamage, ImmutableArray<ModifierData> consumedModifiers, ImmutableArray<ModifierData> remainingModifiers) ApplyDamageModifiers(
            DamageActionResource damageAction,
            EncounterState encounterState)
        {
            CommonValidation.ThrowIfNull(damageAction, nameof(damageAction));
            CommonValidation.ThrowIfNull(encounterState, nameof(encounterState));

            var activeModifiers = encounterState.Spell.ActiveModifiers;
            var baseDamage = GetRawDamage(damageAction, encounterState);

            _logger.LogInfo($"ApplyDamageModifiers: {damageAction.DamageType} damage {damageAction.Amount} with {activeModifiers.Length} modifiers");

            var modifiedDamage = baseDamage;
            var modifiersToRemove = ImmutableArray.CreateBuilder<ModifierData>();

            foreach (var modifier in activeModifiers)
            {
                if (modifier.CanApply(damageAction.DamageType))
                {
                    var oldDamage = modifiedDamage;
                    modifiedDamage = modifier.Apply(modifiedDamage);
                    _logger.LogInfo($"Applied modifier {modifier.Type} {modifier.Value}: {oldDamage} -> {modifiedDamage}");
                    
                    if (modifier.IsConsumedOnUse)
                    {
                        modifiersToRemove.Add(modifier);
                        _logger.LogInfo($"Marked consumable modifier for removal: {modifier.Type} {modifier.Value}");
                    }
                }
            }

            var remainingModifiers = activeModifiers;
            foreach (var modifierToRemove in modifiersToRemove)
            {
                var index = remainingModifiers.IndexOf(modifierToRemove);
                if (index >= 0)
                {
                    remainingModifiers = remainingModifiers.RemoveAt(index);
                }
            }

            var consumedModifiers = modifiersToRemove.ToImmutable();
            _logger.LogInfo($"Final damage: {modifiedDamage}, Consumed {consumedModifiers.Length} modifiers, {remainingModifiers.Length} remaining");
            return (modifiedDamage, consumedModifiers, remainingModifiers);
        }

        public float GetRawDamage(DamageActionResource damageAction, EncounterState encounterState)
        {
            return damageAction.DamageType switch
            {
                DamageType.None => damageAction.Amount,
                DamageType.Fire => damageAction.Amount,
                DamageType.Frost => damageAction.Amount,
                DamageType.PerChill => damageAction.Amount * StatusEffectLogicManager.GetStacksOfEffect(encounterState.StatusEffects, StatusEffectType.Chill),
                _ => damageAction.Amount
            };
        }
    }
}