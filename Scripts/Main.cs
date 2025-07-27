using Godot;
using System;

public partial class Main : Control
{
    public static Main Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public override void _Process(double delta)
    {
    }
}
