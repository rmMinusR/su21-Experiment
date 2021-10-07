using System;
using UnityEngine;

/// <summary>
/// Scripts that take control of movement should implement this for mutual exclusion safety with Rigidbody.
/// </summary>
public abstract class IAction : MonoBehaviour
{
    public enum ExecMode
    {
        Live,
        LiveDelegated, //TODO IMPLEMENT
        SimulatePath,
        SimulateCurves
    }

}

[Serializable]
public sealed class Mutex<T> where T : class
{
    [SerializeReference] [InspectorReadOnly] private OwnedMutex<T> claimant;
    public bool isClaimed => claimant != null;

    private T _owner;
    public T Owner => _owner;

    public OwnedMutex<T> Claim(T byWho)
    {
        if (!isClaimed)
        {
            _owner = byWho;
            return claimant = new OwnedMutex<T>(this);
        }
        else throw new InvalidOperationException();
    }

    public void Release(OwnedMutex<T> by, bool force = false)
    {
        if (isClaimed && (claimant == by || force))
        {
            claimant.Invalidate();
            claimant = null;
            _owner = null;
        }
        else throw new InvalidOperationException();
    }
}

[Serializable]
public sealed class OwnedMutex<T> where T : class
{
    [SerializeReference] [HideInInspector] private Mutex<T> mutex;

    public OwnedMutex(Mutex<T> mutex)
    {
        this.mutex = mutex;
    }

    public void Release()
    {
        mutex.Release(this);
        Invalidate();
    }

    public void Invalidate() => mutex = null;
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