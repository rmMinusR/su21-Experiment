using System;
using UnityEngine;

public abstract class IAbility : ScopedEventListener, IAnimationProvider
{
    private PlayerHost _host;
    protected PlayerHost host => _host != null ? _host : (_host = GetComponent<PlayerHost>());

    public virtual Sprite GetIcon() => null; //FIXME temporary measures
    public virtual string GetName() => this.GetType().Name;

    public abstract void WriteAnimations(PlayerAnimationDriver anim);
}

public abstract class ICastableAbility : IAbility
{
    [SerializeField] [InspectorReadOnly] private bool _currentlyCasting = false;
    protected bool CurrentlyCasting => _currentlyCasting;

    protected void FixedUpdate()
    {
        //TODO how can we make this if block better?
        if (!CurrentlyCasting && ShouldStart())
        {
            if(!EventBus.DispatchEvent(new Events.AbilityTryCastEvent(this)).isCancelled)
            {
                _currentlyCasting = true;
                DoStartCast();
            }
        }
        else if (CurrentlyCasting && ShouldEnd())
        {
            //TODO should we have an event for stopping cast too?
            _currentlyCasting = false;
            DoEndCast();
        }
        else if (CurrentlyCasting)
        {
            DoWhileCasting();
        }
    }

    public abstract bool ShouldStart();
    public virtual void DoStartCast() { }

    public virtual void DoWhileCasting() { }

    public abstract bool ShouldEnd();
    public virtual void DoEndCast() { }
}

public interface IMovementProvider
{
    public Vector2 DoMovement(Vector2 currentVelocity, InputParam input);
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