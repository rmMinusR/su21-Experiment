using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pathfinding
{
    public class SurfaceSweeper
    {
        private List<Surface> surfaces;

        public SurfaceSweeper()
        {
            surfaces = new List<Surface>();
        }

        public void ScanFrom(Vector2 startPos)
        {
            surfaces.Add(Surface.BuildFromSweep(startPos, 0.3f, 0.1f, 0.1f, 45));
        }

        public void DebugDraw()
        {
            foreach (Surface s in surfaces) s.DebugDraw();
        }
    }
}
