using Godot;
using Maximagus.Scripts.Enums;
using Scripts.Commands;
using Scripts.State;
using System;

public partial class StatusEffectIndicator : Control
{
    public StatusEffectType EffectType;
    private RichTextLabel _valueLabel;


    public override void _Ready()
    {
        base._Ready();

        _valueLabel = GetNode<RichTextLabel>("Value");
        var commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        commandProcessor.StateChanged += OnGameStateChanged;
        UpdateFromState(commandProcessor.CurrentState);
    }

    public override void _ExitTree()
    {
        var commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        if (commandProcessor != null)
        {
            commandProcessor.StateChanged -= OnGameStateChanged;
        }
        base._ExitTree();
    }

    public static StatusEffectIndicator Create(StatusEffectType effectType)
    {
        var scene = GD.Load<PackedScene>("res://Scenes/GUI/StatusEffectIndicator.tscn");
        var instance = scene.Instantiate<StatusEffectIndicator>();
        instance.EffectType = effectType;
        return instance;
    }

    private void OnGameStateChanged(IGameStateData oldState, IGameStateData newState)
    {
        UpdateFromState(newState);
    }

    private void UpdateFromState(IGameStateData stateData)
    {
        _valueLabel.Text = stateData.StatusEffects.GetStacksOfEffect(EffectType).ToString();
    }
}
