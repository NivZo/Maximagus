using Godot;

public interface IOrderable
{
    Vector2 TargetPosition { get; set; }
    Vector2 Weight { get; }
    int Z { get; set; }
}