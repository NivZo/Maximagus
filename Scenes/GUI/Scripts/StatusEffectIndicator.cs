using Godot;
using Maximagus.Resources.Definitions.StatusEffects;
using Maximagus.Scripts.Enums;
using Scripts.Commands;
using Scripts.State;
using System;

public partial class StatusEffectIndicator : Control, IOrderable
{
    public StatusEffectType EffectType;

    private RichTextLabel _valueLabel;

    public Vector2 TargetPosition { get; set; }

    public Vector2 Weight => Vector2.One;


    public override void _Ready()
    {
        base._Ready();

        _valueLabel = GetNode<RichTextLabel>("Value");
        var commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        commandProcessor.StateChanged += OnGameStateChanged;
        UpdateFromState(commandProcessor.CurrentState);
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
        if (newState.StatusEffects.GetStacksOfEffect(EffectType) == 0)
        {
            QueueFree();
            return;
        }
        
        UpdateFromState(newState);
    }

    private void UpdateFromState(IGameStateData stateData)
    {
        _valueLabel.Text = stateData.StatusEffects.GetStacksOfEffect(EffectType).ToString();

        if (TargetPosition != default)
        {
            Action<Vector2> setCenter = this.SetCenter;
            CreateTween().SetParallel().TweenMethod(Callable.From(setCenter), this.GetCenter(), TargetPosition, 0.3f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        }
    }
}
