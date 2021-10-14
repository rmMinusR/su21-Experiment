using System;
using UnityEngine;

public abstract class IAbility : ScopedEventListener
{
    private PlayerHost _host;
    protected PlayerHost host => _host != null ? _host : (_host = GetComponent<PlayerHost>());

    public virtual Sprite GetIcon() => null; //FIXME temporary measures
    public virtual string GetName() => this.GetType().Name;
}

public abstract class ICastableAbility : IAbility
{
    //public abstract bool ShowCastingUI(PlayerUIDriver ui);
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