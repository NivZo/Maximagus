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
    private OrderedContainer _indicatorsContainer;
    private ContainerLayoutCache _layoutCache;
    private StatusEffectsState _lastStatusEffectsState;

    public ImmutableArray<StatusEffectIndicator> Indicators => _indicatorsContainer
        ?.Where(n => n is StatusEffectIndicator)
        .Cast<StatusEffectIndicator>()
        .ToImmutableArray() ?? ImmutableArray<StatusEffectIndicator>.Empty;

    public override void _Ready()
    {
        try
        {
            _logger = ServiceLocator.GetService<ILogger>();
            _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
            _layoutCache = new ContainerLayoutCache();

            InitializeComponents();
            SetupEventHandlers();
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error initializing Hand", ex);
            throw;
        }
    }

    private void InitializeComponents()
    {
        _indicatorsContainer = GetNode<OrderedContainer>("OrderedContainer").ValidateNotNull("OrderedContainer");
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
            _indicatorsContainer.RemoveElement(indicator);
        }

        foreach (var statusEffectInstance in toAdd)
        {
            CreateVisualIndicatorByType(statusEffectInstance.EffectType);
        }
    }
    
    private void CreateVisualIndicatorByType(StatusEffectType effectType)
    {
        try
        {
            var indicator = StatusEffectIndicator.Create(effectType);
            _indicatorsContainer.AddChild(indicator);
            _indicatorsContainer.InsertElement(indicator);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Error creating visual indicator", ex);
        }
    }
}
