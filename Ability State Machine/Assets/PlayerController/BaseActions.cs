using System;
using UnityEngine;

/// <summary>
/// Scripts that take control of movement should implement this for mutual exclusion safety with Rigidbody.
/// </summary>
public abstract class IAction : MonoBehaviour
{
#if UNITY_EDITOR
    public abstract Vector2 AllowedSimulatedInterval { get; }
#endif
    public abstract bool AllowEntry { get; }
    public abstract bool AllowExit { get; }

    public abstract void DoSetup(MovementController context, IAction prev, bool isSimulated);
    public abstract void DoCleanup(MovementController context, IAction next, bool isSimulated);
    
    /// <summary>
    /// While active, called as part of every physics update frame to run this
    /// policy's unique code. Must be relatively stateless, only reading variables
    /// that will NEVER change at runtime. This allows it to work both ingame and
    /// for simulating acceleration curves.
    /// 
    /// Rigidbody2D.velocity will stutter if assigned multiple times in the same frame,
    /// and should NEVER be changed directly from this script. Rather, use this method.
    /// </summary>
    /// <param name="context">MovementController host/context</param>
    /// <param name="currentVelocity">Rigidbody's current velocity</param>
    /// <param name="time">Time tracking for this state</param>
    /// <param name="input">Global + surface-local input</param>
    /// <param name="groundedness">0 = airborne, 1 = grounded</param>
    /// <param name="isSimulated">Are we simulating acceleration curves?</param>
    /// <returns>Rigidbody's new velocity</returns>
    public abstract Vector2 DoPhysics(MovementController context, Vector2 currentVelocity, TimeParam time, InputParam input, float groundedness, bool isSimulated);

    /// <summary>
    /// Negative = local-left aka CCW, positive = local-right aka CW.
    /// Only used by animation driver code.
    /// </summary>
    public Facing currentFacing;
}

public enum Facing
{
    Left = -1,
    DontCare = 0,
    Right = 1
}

public static class FacingExt
{
    public static Facing Detect(float input, float threshold)
    {
             if (input < -threshold) return Facing.Left;
        else if (input >  threshold) return Facing.Right;
        else                         return Facing.DontCare;
    }
}

[Serializable]
public struct TimeParam
{
                    public float timeActive;
    [NonSerialized] public float delta;
}

public struct InputParam
{
    [NonSerialized] public Vector2 global;
    [NonSerialized] public Vector2 local;
}