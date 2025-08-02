using System;
using Godot;

public class GodotLogger : ILogger
{
    private bool _isDebug = false;

    public void LogDebug(string message)
    {
        if (_isDebug)
        {
            GD.Print($"[DEBUG] {message}");
        }
    }
    public void LogInfo(string message) => GD.Print($"[INFO] {message}");
    public void LogWarning(string message) => GD.PrintErr($"[WARNING] {message}");
    public void LogError(string message) => GD.PrintErr($"[ERROR] {message}");
    public void LogError(string message, Exception exception) => 
        GD.PrintErr($"[ERROR] {message}: {exception}");
}