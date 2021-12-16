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
        public IReadOnlyList<Vector2> GetPoints() => points;

        [SerializeField] private float maxDistBetweenPoints;

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
            return closestDiff.sqrMagnitude < maxDistBetweenPoints * maxDistBetweenPoints;
        }

        public Surface(Vector2 seedPoint, float maxDistBetweenPoints)
        {
            points = new List<Vector2> { seedPoint };
            this.maxDistBetweenPoints = maxDistBetweenPoints;
        }

        public static Surface BuildFromSweep(Vector2 startPoint, float mergeEpsilon, float stepEpsilon, float backpedalEpsilon, float maxSurfaceAngle, Func<GameObject, bool> ignore)
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
                scan.distance = float.PositiveInfinity;
                foreach (RaycastHit2D h in Physics2D.RaycastAll(scan.point, -scan.normal)) if (h.distance < scan.distance && !ignore(h.collider.gameObject)) scan = h;

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
                scan.distance = float.PositiveInfinity;
                foreach(RaycastHit2D h in Physics2D.RaycastAll(scan.point, -scan.normal)) if(h.distance < scan.distance && !ignore(h.collider.gameObject)) scan = h;

                @continue &= scan.distance > 0.01f && Vector2.Angle(Vector2.up, scan.normal) < maxSurfaceAngle && surf.IsNear(scan.point);
                if(@continue) surf.points.Add(scan.point);
            }
            
            return surf;
        }

        public void DebugDraw(float maxAngle)
        {
            for(int i = 1; i < points.Count; ++i)
            {
                Vector2 a = points[i - 1], b = points[i];
                Vector2 diff = a-b;
                float ang = Vector2.Angle(Vector2.right, diff.x<0 ? -diff : diff);

                Handles.color = Color.HSVToRGB(Mathf.Lerp(0.33f, 0, ang/maxAngle), 1, 1);
                Handles.DrawAAPolyLine(10.0f, a, b);
            }
        }
    }
}
