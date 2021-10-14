using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class Vector2Ext
{
    public static Vector2 Proj(Vector2 from, Vector2 onto)
    {
        return Vector2.Dot(from, onto) / onto.sqrMagnitude * onto;
    }
}

public static class GeneralExt
{
    public static float Sq(this float v) => v * v;

    public static double Sq(this double v) => v * v;

    public static decimal Sq(this decimal v) => v * v;
}

public static class DataStructExt
{
    public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
    {
        if (!dict.TryGetValue(key, out TValue val))
        {
            val = new TValue();
            dict.Add(key, val);
        }

        return val;
    }

    public static WrappedIEnumerator<T> AsEnumerable<T>(this IEnumerator<T> e) => new WrappedIEnumerator<T>(e);
}

public class WrappedIEnumerator<T> : IEnumerable<T>
{
    private IEnumerator<T> enumerator;

    public WrappedIEnumerator(IEnumerator<T> enumerator)
    {
        this.enumerator = enumerator;
    }

    public IEnumerator<T> GetEnumerator()
    {
        enumerator.Reset();
        return enumerator;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

public static class RandomExt
{
    public static T Choice<T>(T[] src) where T : class => src.Length > 0 ? src[UnityEngine.Random.Range(0, src.Length)] : null;
}

public static class EditorExt
{
    public delegate void FuncDrawLine(Vector3 p1, Vector3 p2);
    public delegate void FuncSetColor(Color c);

    public static void DrawArrowHandles(Vector3 from, Vector3 to) => DrawArrow(from, to, Vector3.forward, Handles.DrawLine);
    public static void DrawArrowGizmos(Vector3 from, Vector3 to) => DrawArrow(from, to, Vector3.forward, Gizmos.DrawLine);
    public static void DrawArrow(Vector3 from, Vector3 to, Vector3 relUp, FuncDrawLine drawLine)
    {
        float headSize = (from-to).magnitude * 0.25f;
        Vector3 headBase = (from-to).normalized * headSize;

        Vector3 head1 = to;
        Vector3 head2 = to + Quaternion.AngleAxis(-45, relUp) * headBase;
        Vector3 head3 = to + Quaternion.AngleAxis( 45, relUp) * headBase;

        drawLine(from, to);
        drawLine(head1, head2);
        drawLine(head2, head3);
        drawLine(head3, head1);
    }

    public static void DrawPolyLine(bool closed, params Vector3[] points)
    {
        for(int i = 0; i < points.Length-(closed?0:1); i++)
        {
            Gizmos.DrawLine(points[i], points[(i+1)%points.Length]);
        }
    }
}

public static class LINQExt
{
    public static T NullsafeFirstC<T>(this IEnumerable<T> src) where T : class
    {
        if(src.Count() > 0) return src.First();
        else return null;
    }

    public static T? NullsafeFirstS<T>(this IEnumerable<T> src) where T : struct
    {
        if(src.Count() > 0) return src.First();
        else return null;
    }
}

//From https://stackoverflow.com/a/2012855
public class NTree<T> : IEnumerable<NTree<T>>
{
    public delegate void Visitor(NTree<T> nodeData);

    public T data;
    public LinkedList<NTree<T>> children;

    public NTree(T data)
    {
        this.data = data;
        children = new LinkedList<NTree<T>>();
    }

    public void Traverse(Visitor visitor)
    {
        visitor(this);
        foreach (NTree<T> child in children) child.Traverse(visitor);
    }

    public NTree<T> Find(System.Func<NTree<T>, bool> predicate)
    {
        NTree<T> output = null;
        
        Traverse(
            x => {
                if (predicate(x)) output = x;
            }
        );

        return output;
    }

    public IEnumerator<NTree<T>> GetEnumerator()
    {
        //Evaluate
        HashSet<NTree<T>> vals = new HashSet<NTree<T>>();
        Traverse(x => vals.Add(x));
        return vals.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        //Evaluate
        HashSet<NTree<T>> vals = new HashSet<NTree<T>>();
        Traverse(x => vals.Add(x));
        return vals.GetEnumerator();
    }
}

[Serializable]
public sealed class Mutex<T> where T : class
{
    private T _owner;
    public T Owner => _owner;
    public bool isClaimed => _owner != null;

    public OwnedMutex<T> Claim(T byWho)
    {
        if (!isClaimed)
        {
            _owner = byWho;
            return new OwnedMutex<T>(this);
        }
        else throw new InvalidOperationException();
    }

    public void Release(OwnedMutex<T> by, bool force = false)
    {
        if (isClaimed && (Owner == by.Owner || force))
        {
            _owner = null;
            by.Invalidate();
        }
        else throw new InvalidOperationException();
    }
}

[Serializable]
public sealed class OwnedMutex<T> where T : class
{
    [SerializeField] [HideInInspector] private Mutex<T> mutex;
    [SerializeField] private T _owner;
    public T Owner => _owner;

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

public static class InputExt
{
    public static bool ReadAsButton(this UnityEngine.InputSystem.InputAction input) => input.ReadValue<float>() > 0.5f;
}