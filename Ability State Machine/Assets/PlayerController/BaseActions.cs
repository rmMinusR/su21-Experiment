using System;
using UnityEngine;

/// <summary>
/// Scripts that take control of movement should implement this for mutual exclusion safety with Rigidbody.
/// </summary>
public abstract class IAction : MonoBehaviour
{
    public enum PhysicsMode
    {
        Live,
        SimulatePath,
        SimulateCurves
    }

#if UNITY_EDITOR
    public abstract Vector2 AllowedSimulatedInterval { get; }
#endif

    public abstract bool AllowEntry { get; }
    public abstract bool AllowExit { get; }

    /// <summary>
    /// Called when first taking effect. Do any setup code here. Not necessarily on the same frame as a call to DoPhysics.
    /// </summary>
    /// <param name="context">Host context</param>
    /// <param name="prev">Previously-active action</param>
    /// <param name="mode">Are we live, or simulating?</param>
    public abstract void DoSetup(MovementController.Context context, IAction prev, PhysicsMode mode);

    /// <summary>
    /// Called when no longer taking effect. Do any cleanup code here. Not necessarily on the same frame as a call to DoPhysics.
    /// </summary>
    /// <param name="context">Host context</param>
    /// <param name="prev">Previously-active action</param>
    /// <param name="mode">Are we live, or simulating?</param>
    public abstract void DoCleanup(MovementController.Context context, IAction next, PhysicsMode mode);
    
    /// <summary>
    /// While active, called as part of every physics update frame to run this
    /// policy's unique code. Must be relatively stateless, only reading variables
    /// that will NEVER change at runtime. This allows it to work both ingame and
    /// for simulating acceleration curves.
    /// 
    /// Rigidbody2D.velocity will stutter if assigned multiple times in the same frame,
    /// and should NEVER be changed directly from this script. Rather, use this method.
    /// </summary>
    /// <param name="context">Host context</param>
    /// <param name="currentVelocity">Rigidbody's current velocity</param>
    /// <param name="mode">Are we live, or simulating?</param>
    /// <returns>Rigidbody's new velocity</returns>
    public abstract Vector2 DoPhysics(MovementController.Context context, Vector2 currentVelocity, PhysicsMode mode);

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
    public float stable;
    public float active;
    public float delta;
}

[Serializable]
public struct InputParam
{
    public Vector2 global;
    public Vector2 local;
    public bool jump;
}