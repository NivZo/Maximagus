using System;
using System.Collections.Generic;
using Godot;

public static class TweenUtils
{
    private static Dictionary<string, (Tween Tween, Variant Value)> _tweens = new();

    public static Tween Pop(Node node, float scale, float duration = 1f, Tween.TransitionType transitionType = Tween.TransitionType.Elastic, Tween.EaseType easeType = Tween.EaseType.Out)
        => AddPropertyTween(node, "scale", new Vector2(scale, scale), duration, transitionType, easeType);

    public static Tween Travel(Node node, Vector2 to, float duration = 0.5f, Tween.TransitionType transitionType = Tween.TransitionType.Quint, Tween.EaseType easeType = Tween.EaseType.Out)
        => AddPropertyTween(node, "global_position", to, duration, transitionType, easeType);

    public static void Color(Node node, Color to, float duration = 1f, Tween.TransitionType transitionType = Tween.TransitionType.Cubic, Tween.EaseType easeType = Tween.EaseType.Out)
    {
        if (node is ColorRect colorRect)
        {
            AddPropertyTween(colorRect, "color", to, duration, transitionType, easeType);
        }
        else if (node is TextureRect textureRect && textureRect.Texture is GradientTexture2D gradientTexture)
        {
            var from = gradientTexture.Gradient.GetColor(0);
            MethodTween(textureRect, val =>
            {
                gradientTexture.Gradient.SetColor(0, val.AsColor());
                gradientTexture.Gradient.SetColor(1, val.AsColor().Darkened(.15f));
            }, from, to, duration, transitionType, easeType);
            AddPropertyTween(textureRect, "self_modulate", to, duration, transitionType, easeType);
        }
        else
        {
            AddPropertyTween(node, "self_modulate", to, duration, transitionType, easeType);
        }
    }

    public static Tween MethodTween(Node node, Action<Variant> action, Variant from, Variant to, float duration, Tween.TransitionType transitionType = Tween.TransitionType.Cubic, Tween.EaseType easeType = Tween.EaseType.Out)
    {
        var tween = AddCachedTween(node, action.Method.Name, to, transitionType, easeType);
        if (tween != null)
        {
            tween.TweenMethod(Callable.From(action), from, to, duration);
        }

        return tween;
    }

    private static Tween AddPropertyTween(Node node, string property, Variant value, float duration, Tween.TransitionType transitionType, Tween.EaseType easeType)
    {
        var tween = AddCachedTween(node, property, value, transitionType, easeType);
        if (tween != null)
        {
            tween.TweenProperty(node, property, value, duration);
        }

        return tween;
    }

    private static Tween AddCachedTween(Node node, string property, Variant value, Tween.TransitionType transitionType, Tween.EaseType easeType = Tween.EaseType.Out)
    {
        if (node != null && !node.IsQueuedForDeletion())
        {
            var tweenKey = $"{node.GetInstanceId()}-{property}";
            var exists = _tweens.TryGetValue(tweenKey, out var tweenTuple) && tweenTuple.Tween.IsRunning();

            if (exists && !tweenTuple.Value.Equals(value))
            {
                tweenTuple.Tween.Kill();
            }

            var tween = node?
                .CreateTween()
                .SetEase(easeType)
                .SetTrans(transitionType)
                .SetParallel(true);

            if (tween != null)
            {
                _tweens.Remove(tweenKey);
                _tweens.Add(tweenKey, (tween, value));
            }

            return tween;
        }

        return null;
    }
}