using System;
using UnityEngine;

/// <summary>
/// Scripts that take control of movement should implement this for mutual exclusion safety with Rigidbody.
/// </summary>
public interface IAction
{
    public enum ExecMode
    {
        Live,
        LiveDelegated, //TODO IMPLEMENT
        SimulatePath,
        SimulateCurves
    }

#if UNITY_EDITOR
    public Vector2 AllowedSimulatedInterval { get; }
#endif

    public bool AllowEntry(in PlayerHost.Context context);
    public bool AllowExit(in PlayerHost.Context context);

    /// <summary>
    /// Called when first taking effect. Do any setup code here. Not necessarily on the same frame as a call to DoPhysics.
    /// </summary>
    /// <param name="context">Host context</param>
    /// <param name="prev">Previously-active action</param>
    /// <param name="mode">Are we live, or simulating?</param>
    public void DoSetup(ref PlayerHost.Context context, IAction prev, ExecMode mode);
    
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
    public Vector2 DoPhysics(ref PlayerHost.Context context, Vector2 currentVelocity, ExecMode mode);

    /// <summary>
    /// Called when no longer taking effect. Do any cleanup code here. Not necessarily on the same frame as a call to DoPhysics.
    /// </summary>
    /// <param name="context">Host context</param>
    /// <param name="prev">Previously-active action</param>
    /// <param name="mode">Are we live, or simulating?</param>
    public void DoCleanup(ref PlayerHost.Context context, IAction next, ExecMode mode);
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