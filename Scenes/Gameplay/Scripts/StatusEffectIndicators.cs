using Godot;
using System;
using System.Collections.Immutable;
using System.Linq;
using Scripts.Commands;
using Scripts.State;
using Scripts.Utils;
using Maximagus.Scripts.Enums;

public partial class StatusEffectIndicators : Control
{
    private ILogger _logger;
    private IGameCommandProcessor _commandProcessor;
    private StatusEffectsState _lastStatusEffectsState;

    public ImmutableArray<StatusEffectIndicator> Indicators => GetChildren().OfType<StatusEffectIndicator>().ToImmutableArray();

    public override void _Ready()
    {
        try
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();

            SetupEventHandlers();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error initializing Hand", ex);
            throw;
        }
    }

    private void SetupEventHandlers()
    {
        _commandProcessor.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(IGameStateData oldGlobalState, IGameStateData newGlobalState)
    {
        var newState = newGlobalState.StatusEffects;
        try
        {
            // Use more reliable comparison - check if the actual indicator lists differ
            bool containerStateChanged = _lastStatusEffectsState == null || !_lastStatusEffectsState.Equals(newState);

            if (containerStateChanged)
            {
                _lastStatusEffectsState = newState;
                SyncVisualIndicatorsWithState(_lastStatusEffectsState);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error handling hand state change", ex);
        }
    }

    private void SyncVisualIndicatorsWithState(StatusEffectsState currentState)
    {
        var currentIndicators = Indicators.ToArray();
        var orderedStateIndicators = currentState.ActiveEffects.OrderBy(c => c.CurrentStacks).ToArray();

        var toRemove = currentIndicators.Where(indicator => !orderedStateIndicators.Any(c => c.EffectType == indicator.EffectType && c.CurrentStacks > 0)).ToList();
        var toAdd = orderedStateIndicators.Where(c => !currentIndicators.Any(indicator => indicator.EffectType == c.EffectType)).ToList();

        foreach (var indicator in toRemove)
        {
            indicator.QueueFree();
        }

        foreach (var statusEffectInstance in toAdd)
        {
            CreateVisualIndicatorByType(statusEffectInstance.EffectType);
        }

        var yOffset = 24;
        for (int i = 0; i < Indicators.Length; i++)
        {
            var indicator = Indicators[i];
            indicator.SetCenter(this.GetCenter() with { Y = 0 + yOffset });
            yOffset += (int)indicator.Size.Y + 5;
        }
    }
    
    private void CreateVisualIndicatorByType(StatusEffectType effectType)
    {
        try
        {
            var indicator = StatusEffectIndicator.Create(effectType);
            AddChild(indicator);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error creating visual indicator", ex);
        }
    }
}
