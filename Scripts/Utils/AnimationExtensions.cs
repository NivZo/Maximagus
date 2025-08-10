using Godot;
using System;

public static class AnimationExtensions
{
    private const string SCALE_TWEEN_META_KEY = "__scale_tween";

    public static Tween AnimateScale(this Control node, float targetScale, float duration, Tween.TransitionType transitionType)
    {
        var existingTween = node.GetRunningScaleTween();
        existingTween?.Kill();

        Tween newTween = node.CreateTween();

        node.SetMeta(SCALE_TWEEN_META_KEY, newTween);

        Action<float> setScaleAction = (s) => node.Scale = new Vector2(s, s);

        newTween.SetEase(Tween.EaseType.Out)
                .SetTrans(transitionType);

        newTween.TweenMethod(Callable.From(setScaleAction), node.Scale.X, targetScale, duration);

        newTween.Finished += () =>
        {
            if (node.HasMeta(SCALE_TWEEN_META_KEY))
            {
                node.RemoveMeta(SCALE_TWEEN_META_KEY);
            }
        };

        return newTween;
    }

    public static Tween GetRunningScaleTween(this Control node)
    {
        if (node.HasMeta(SCALE_TWEEN_META_KEY) && node.GetMeta(SCALE_TWEEN_META_KEY).Obj is Tween existingTween && existingTween.IsValid())
        {
            return existingTween;
        }

        return null;
    }
}