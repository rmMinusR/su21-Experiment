using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pathfinding
{
    public class WorldRepresentation
    {
        private List<Surface> surfaces;

        public WorldRepresentation()
        {
            surfaces = new List<Surface>();
        }

        public void ScanAll(float horizontalScanStep, float mergeDist, float sweepStep, float sweepBackpedal, float maxSurfaceAngle, Func<GameObject, bool> ignore)
        {
            //Build world bounds
            Bounds worldBounds = new Bounds();
            foreach(Collider2D c in UnityEngine.Object.FindObjectsOfType<Collider2D>()) worldBounds.Encapsulate(c.bounds);

            //Vertical scan through all, ensuring we capture all surfaces
            for(Vector2 scanOrigin = worldBounds.max; scanOrigin.x >= worldBounds.min.x; scanOrigin.x -= horizontalScanStep)
            {
                foreach(RaycastHit2D hit in Physics2D.RaycastAll(scanOrigin, Vector2.down, worldBounds.size.y))
                {
                    //Check if should be ignored, or if already covered by an existing Surface
                    if(!ignore(hit.collider.gameObject) && !surfaces.Any(s => s.IsNear(hit.point)))
                    {
                        surfaces.Add(Surface.BuildFromSweep(hit.point, mergeDist, sweepStep, sweepBackpedal, maxSurfaceAngle, ignore));
                    }
                }
            }
        }

        public void DebugDraw()
        {
            foreach (Surface s in surfaces) s.DebugDraw();
        }
    }
}
