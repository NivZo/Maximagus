using Godot;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class Tooltip : Control
{
    private const string SCENE_PATH = "res://Scenes/GUI/Tooltip.tscn";

    private RichTextLabel _contentLabel;
    private TextureRect _middle;
    private TextureRect _bottom;
    private Func<Vector2> _getPosition;

    private const int CONTENT_FONT_SIZE = 12;

    public override void _Ready()
    {
        base._Ready();
        PivotOffset = Size / 2f;

        _contentLabel = GetNode<RichTextLabel>("ContentLabel");
        _middle = GetNode<TextureRect>("TooltipMiddle");
        _bottom = GetNode<TextureRect>("TooltipBottom");
    }

    public static Tooltip Create(Func<Vector2> getPosition, string title, string content)
    {
        var tooltip = GD.Load<PackedScene>(SCENE_PATH).Instantiate<Tooltip>();
        ServiceLocator.GetService<CardsRoot>().AddChild(tooltip);
        tooltip.GetNode<RichTextLabel>("TooltipTop/TitleLabel").Text = title;
        tooltip.GetNode<RichTextLabel>("ContentLabel").Text = content;
        tooltip.FitHeight();
        tooltip.Visible = false;
        tooltip._getPosition = getPosition;

        return tooltip;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Visible)
        {
            GlobalPosition = _getPosition();
        }
    }

    public void ShowTooltip()
    {
        GlobalPosition = _getPosition();

        if (!Visible)
        {
            // Scale = new(.4f, .4f);
            Visible = true;
            // this.AnimateScale(1, .4f, Tween.TransitionType.Elastic);
        }

    }

    public void HideTooltip()
    {
        Visible = false;
    }

    private void FitHeight()
    {
        var heightDelta = _contentLabel.GetContentHeight() - _contentLabel.Size.Y + CONTENT_FONT_SIZE;
        if (heightDelta > 0)
        {
            _middle.Size = _middle.Size with { Y = heightDelta };
            _bottom.Position = _bottom.Position with { Y = _bottom.Position.Y + heightDelta };
            _contentLabel.SetSize(_contentLabel.Size with { Y = _contentLabel.Size.Y + heightDelta });
            // _contentLabel.Size = _contentLabel.Size with { Y = _contentLabel.Size.Y + heightDelta };
            Position = Position with { Y = Position.Y - heightDelta };
        }

    }
}
