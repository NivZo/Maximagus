using Godot;

public partial class EffectPopUp : Control
{
    private const string SCENE_PATH = "res://Scenes/GUI/EffectPopUp.tscn";
    private const float DisplayDuration = 1f;

    private RichTextLabel _contentLabel;
    private ColorRect _bg;
    private GpuParticles2D _particles;

    private const int CONTENT_FONT_SIZE = 12;

    public override void _Ready()
    {
        base._Ready();
        PivotOffset = Size / 2f;

        _bg = GetNode<ColorRect>("Background");
        _particles = GetNode<GpuParticles2D>("Particles");
        _contentLabel = GetNode<RichTextLabel>("ContentLabel");
    }

    public static EffectPopUp Create(Vector2 globalPosition, string content)
    {
        var effectPopUp = GD.Load<PackedScene>(SCENE_PATH).Instantiate<EffectPopUp>();
        ServiceLocator.GetService<CardsRoot>().AddChild(effectPopUp);
        effectPopUp.GlobalPosition = globalPosition - effectPopUp.Size / 2f;
        effectPopUp.GetNode<RichTextLabel>("ContentLabel").Text = content;
        effectPopUp.Visible = false;

        effectPopUp.ShowEffectPopUp();

        return effectPopUp;
    }

    public void ShowEffectPopUp()
    {
        Scale = new(.8f, .8f);
        Visible = true;
        _particles.Emitting = true;
        _bg.Rotation = (GD.Randf() * 2 - 1) * Mathf.Pi * .1f;

        this.AnimateScale(1, .3f, Tween.TransitionType.Elastic);
        TimerUtils.ExecuteAfter(HideEffectPopUp, DisplayDuration);
    }

    public void HideEffectPopUp()
    {
        this.AnimateScale(0, .3f, Tween.TransitionType.Cubic).Finished += QueueFree;
    }
}
