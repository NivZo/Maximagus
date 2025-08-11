using System;
using Godot;

public static class TimerUtils
{
    public static void ExecuteAfter(Action action, float delay)
    {
        var timer = (Engine.GetMainLoop() as SceneTree).Root.GetTree().CreateTimer(delay);
        timer.Timeout += action;
    }
}
