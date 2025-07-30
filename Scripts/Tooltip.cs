using Godot;
using System;

public partial class Tooltip : Control
{
    private const string SCENE_PATH = "res://Scenes/Tooltip.tscn";

    public override void _Ready()
    {
        base._Ready();
    }

    public static Tooltip Create(Node parent, Vector2 offset, string title, string content)
    {
        var tooltip = GD.Load<PackedScene>(SCENE_PATH).Instantiate<Tooltip>();
        parent.AddChild(tooltip);
        tooltip.Position = offset;

        return tooltip;
    }
}
