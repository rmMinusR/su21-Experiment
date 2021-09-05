using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Vector2Ext
{
    public static Vector2 Proj(Vector2 from, Vector2 onto)
    {
        return Vector2.Dot(from, onto) / onto.magnitude.Sq() * onto;
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

public static class GizmosExt
{
    public static void DrawArrow(Vector3 from, Vector3 to) => DrawArrow(from, to, Vector3.forward);
    public static void DrawArrow(Vector3 from, Vector3 to, Vector3 relUp)
    {
        float headSize = (from-to).magnitude * 0.4f;
        Vector3 headBase = (from-to).normalized * headSize;

        Gizmos.DrawLine(from, to);
        DrawPolyLine(true,
                to,
                to + Quaternion.AngleAxis(-45, relUp) * headBase,
                to + Quaternion.AngleAxis( 45, relUp) * headBase
            );
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