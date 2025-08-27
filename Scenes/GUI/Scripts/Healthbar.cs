using Godot;
using Scripts.Commands;
using Scripts.State;

public partial class Healthbar : Control
{
    private TextureProgressBar _fill;
    private TextureProgressBar _fillFlash;
    private RichTextLabel _value;

    private IGameCommandProcessor _commandProcessor;

    public override void _Ready()
    {
        base._Ready();

        _fill = GetNode<TextureProgressBar>("Fill");
        _fillFlash = GetNode<TextureProgressBar>("FillFlash");
        _value = GetNode<RichTextLabel>("Value");

        _commandProcessor = ServiceLocator.GetService<IGameCommandProcessor>();
        _commandProcessor.StateChanged += OnStatusChanged;
    }

    private void OnStatusChanged(IGameStateData oldState, IGameStateData newState)
    {
        if (oldState.Enemy.Equals(newState.Enemy)) return;

        _fill.Value = (float)newState.Enemy.CurrentHealth / newState.Enemy.MaxHealth;
        _value.Text = $"{newState.Enemy.CurrentHealth} / {newState.Enemy.MaxHealth}";

        TimerUtils.ExecuteAfter(() => AnimateFill(_fill.Value), .5f);
    }

    private void AnimateFill(double targetValue)
    {
        TweenUtils.MethodTween(this, value => _fillFlash.Value = value.AsDouble(), _fillFlash.Value, targetValue, 1, Tween.TransitionType.Expo, Tween.EaseType.Out);
    }
}
