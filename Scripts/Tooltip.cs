using Godot;
using System;

public partial class Tooltip : Control
{
    private const string SCENE_PATH = "res://Scenes/GUI/Tooltip.tscn";

    public override void _Ready()
    {
        base._Ready();

        var texture = GetNode<TextureRect>("Background");
        var textureBehind = texture.GetNode<TextureRect>("DarkBackground");
        PivotOffset = Size / 2f;
        texture.PivotOffset = texture.Size / 2f;
        textureBehind.PivotOffset = textureBehind.Size / 2f;
    }

    public static Tooltip Create(Node parent, Vector2 offset, string title, string content)
    {
        var tooltip = GD.Load<PackedScene>(SCENE_PATH).Instantiate<Tooltip>();
        parent.AddChild(tooltip);
        tooltip.Position = offset;

        return tooltip;
    }

    public void ShowTooltip()
    {
        Scale = new(.9f, .9f);
        Visible = true;
        AnimationUtils.AnimateScale(this, 1, .5f, Tween.TransitionType.Elastic);
    }

    public void HideTooltip()
    {
        Visible = false;
    }
}
