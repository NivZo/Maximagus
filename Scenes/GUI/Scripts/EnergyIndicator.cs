using Godot;
using Scripts.Commands;
using Scripts.State;
using System;

public partial class EnergyIndicator : Control
{
    private RichTextLabel _valueLabel;
    public override void _Ready()
    {
        base._Ready();

        _valueLabel = GetNode<RichTextLabel>("Value");
        var commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        commandProcessor.StateChanged += OnGameStateChanged;
    }

    private void OnGameStateChanged(IGameStateData oldState, IGameStateData newState)
    {
        _valueLabel.Text = newState.Player.RemainingHands.ToString();
    }
}
