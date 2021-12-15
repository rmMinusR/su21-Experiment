using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pathfinding
{
    [Serializable]
    public class Surface
    {
        [SerializeField] private List<Vector2> points; //Basically a polyline

        [SerializeField] private float distBetweenPoints;

        public void GetClosestPoint(Vector2 from, out Vector2 closest, out int closestInd)
        {
            closestInd = 0;
            float closestDistSq = (points[0]-from).sqrMagnitude;

            for(int i = 1; i < points.Count; ++i)
            {
                float distSq = (points[i]-from).sqrMagnitude;
                if(distSq < closestDistSq)
                {
                    closestInd = i;
                    closestDistSq = distSq;
                }
            }

            closest = points[closestInd];
        }

        public bool IsNear(Vector2 point)
        {
            GetClosestPoint(point, out Vector2 closestPoint, out _);
            Vector2 closestDiff = point-closestPoint;
            return closestDiff.sqrMagnitude < distBetweenPoints * distBetweenPoints;
        }

        public bool TryMerge(Vector2 point)
        {
            if(points.Contains(point)) throw new InvalidOperationException(); //Refuse to merge something that's already here

            GetClosestPoint(point, out Vector2 closestPoint, out int closestInd);
            Vector2 closestDiff = point-closestPoint;
            if (closestDiff.sqrMagnitude > distBetweenPoints * distBetweenPoints) return false; //Too far to merge

            Vector2 before = points[Mathf.Max(closestInd-1, 0             )];
            Vector2 after  = points[Mathf.Min(closestInd+1, points.Count-1)];
            Vector2 tangent = after-before;

            bool insertBefore = Vector2.Dot(tangent, closestDiff) < 0;
            int insertLocation = closestInd + (insertBefore?1:0);

            points.Insert(insertLocation, point);
            return true;
        }

        public Surface(Vector2 seedPoint, float epsilon)
        {
            points = new List<Vector2> { seedPoint };
            this.distBetweenPoints = epsilon;
        }

        public static Surface BuildFromSweep(Vector2 startPoint, float mergeEpsilon, float stepEpsilon, float backpedalEpsilon, float maxSurfaceAngle)
        {
            RaycastHit2D initialScan = Physics2D.Raycast(startPoint, Vector2.down);
            Surface surf = new Surface(initialScan.point, mergeEpsilon);

            //Scan left until we can no longer merge
            RaycastHit2D scan = initialScan;
            bool @continue = true;
            while(@continue)
            {
                Vector2 tangentLeft = new Vector2(-scan.normal.y, scan.normal.x);
                scan.point += tangentLeft * stepEpsilon + scan.normal * backpedalEpsilon;
                scan = Physics2D.Raycast(scan.point, -scan.normal);

                @continue &= scan.distance > 0.01f && Vector2.Angle(Vector2.up, scan.normal) < maxSurfaceAngle && surf.IsNear(scan.point);
                if(@continue) surf.points.Insert(0, scan.point);
            }

            //Scan right until we can no longer merge
            scan = initialScan;
            @continue = true;
            while(@continue)
            {
                Vector2 tangentRight = new Vector2(scan.normal.y, -scan.normal.x);
                scan.point += tangentRight * stepEpsilon + scan.normal * backpedalEpsilon;
                scan = Physics2D.Raycast(scan.point, -scan.normal);

                @continue &= scan.distance > 0.01f && Vector2.Angle(Vector2.up, scan.normal) < maxSurfaceAngle && surf.IsNear(scan.point);
                if(@continue) surf.points.Add(scan.point);
            }
            
            return surf;
        }

        public void DebugDraw()
        {
            Handles.color = Color.red;
            for(int i = 1; i < points.Count; ++i)
            {
                Handles.DrawAAPolyLine(10.0f, points[i-1], points[i]);
            }
        }
    }
}
