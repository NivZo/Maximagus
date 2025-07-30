using Godot;
using System;

public partial class Parallax : Control
{
    [Export] public Vector2 MaxOffset;
    [Export] public float Smoothing = 2;

    public override void _Process(double delta)
    {
        base._Process(delta);

        var center = GetViewportRect().GetCenter();
        var dist = GetGlobalMousePosition() - center;
        var offset = dist / center;

        var newPos = new Vector2(
            Mathf.Lerp(MaxOffset.X, -MaxOffset.X, offset.X),
            Mathf.Lerp(MaxOffset.Y, -MaxOffset.Y, offset.Y)
        );

        Position = new(
            Mathf.Lerp(Position.X, newPos.X, Smoothing * (float)delta),
            Mathf.Lerp(Position.Y, newPos.Y, Smoothing * (float)delta)
        );
    }
}
