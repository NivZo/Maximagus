using Godot;

public static class ControlExtensions
{
    public static Vector2 GetCenter(this Control control)
    {
        if (control == null) return Vector2.Zero;
        return control.GlobalPosition + control.Size / 2f;
    }

    public static void SetCenter(this Control control, Vector2 position)
    {
        if (control == null) return;
        control.GlobalPosition = position - control.Size / 2f;
    }
}