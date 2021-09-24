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
}

public static class RandomExt
{
    public static T Choice<T>(T[] src) where T : class => src.Length > 0 ? src[Random.Range(0, src.Length)] : null;
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
public class NTree<T>
{
    public delegate void Visitor(NTree<T> nodeData);

    public T data { get; private set; }
    private LinkedList<NTree<T>> children;

    public NTree(T data)
    {
        this.data = data;
        children = new LinkedList<NTree<T>>();
    }

    public void AddChild(T data)
    {
        children.AddFirst(new NTree<T>(data));
    }

    public NTree<T> GetChild(int i)
    {
        foreach (NTree<T> n in children)
            if (--i == 0)
                return n;
        return null;
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
}