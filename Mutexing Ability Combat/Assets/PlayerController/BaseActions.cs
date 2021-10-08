using System;
using UnityEngine;

public abstract class IAction : ScopedEventListener
{
    public enum ExecMode //TODO remove
    {
        Live,
        LiveDelegated,
        SimulatePath,
        SimulateCurves
    }

    public abstract Sprite GetIcon();
    public abstract string GetName();
}

public abstract class ICastableAbility : IAction
{
    public abstract bool ShowCastingUI(PlayerUIDriver ui);
}

public enum Facing
{
    Left = -1,
    Agnostic = 0,
    Right = 1
}

public static class FacingExt
{
    public static Facing Detect(float input, float threshold)
    {
             if (input < -threshold) return Facing.Left;
        else if (input >  threshold) return Facing.Right;
        else                         return Facing.Agnostic;
    }
}

[Serializable]
public struct TimeParam
{
    public float stable;
    public float delta;
}

[Serializable]
public struct InputParam
{
    public Vector2 global;
    public Vector2 local;
    public bool jump;
}