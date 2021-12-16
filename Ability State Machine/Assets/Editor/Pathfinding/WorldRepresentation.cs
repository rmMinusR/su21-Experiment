using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pathfinding
{
    [Serializable]
    public class WorldRepresentation
    {
        [SerializeField] private List<Surface> surfaces;
        [SerializeField] private Bounds worldBounds;
        
        public Bounds WorldBounds => worldBounds;

        public WorldRepresentation(float horizontalScanStep, float mergeDist, float sweepStep, float sweepBackpedal, float maxSurfaceAngle, Func<GameObject, bool> ignore)
        {
            surfaces = new List<Surface>();

            //Build world bounds
            worldBounds = new Bounds();
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

        public List<Connection> GetConnections(Pathfinder pathfinder)
        {
            List<Connection> connections = new List<Connection>();
            foreach(Surface fromSurf in surfaces)
            {
                void TryAirborneArc(InputParam input, Vector2 startPoint, int startIndex, Vector2 startVel)
                {
                    //Setup context
                    PlayerHost.Context context = pathfinder.character.context;
                    context.input = input;

                    //Run simulation forward
                    List<PhysicsSimulator.Frame> path = new List<PhysicsSimulator.Frame> { new PhysicsSimulator.Frame { pos = startPoint, grounded = true, time = 0 } };
                    pathfinder.physicsSimulator.SimulateSegmentForward(
                        pathfinder.character,
                        pathfinder.character.context,
                        ref path,
                        (prev, next) => !worldBounds.Contains(next.pos)
                                        || (next.grounded && !prev.grounded),
                        x => input
                    );
                    Vector2 simulationEnd = path[path.Count-1].pos;

                    //Check if we hit a walkable surface
                    Surface toSurf = surfaces.Where(s => s.IsNear(simulationEnd)).NullsafeFirstC();
                    if (toSurf != null)
                    {
                        toSurf.GetClosestPoint(simulationEnd, out Vector2 arcEndPoint, out int arcEndIndex);
                        connections.Add(new Connection(
                            new Connection.Node { surface = fromSurf, index = startIndex , point = startPoint },
                            new Connection.Node { surface = toSurf,   index = arcEndIndex, point = arcEndPoint},
                            input,
                            path
                        ));
                    }
                }

                //Fall-off-edge arcs
                {
                    int leftInd = 0;
                    Vector2 leftEdge = fromSurf.GetPoints()[leftInd];
                    int rightInd = fromSurf.GetPoints().Count-1;
                    Vector2 rightEdge = fromSurf.GetPoints()[rightInd];
                    if (leftEdge.x > rightEdge.x)
                    {
                        GeneralExt.Swap(ref leftEdge, ref rightEdge);
                        GeneralExt.Swap(ref leftInd, ref rightInd);
                    }
                    TryAirborneArc(new InputParam { global = Vector2.left , local = Vector2.left , jump = true }, leftEdge , leftInd , Vector2.left *pathfinder.movement.moveSpeed);
                    TryAirborneArc(new InputParam { global = Vector2.right, local = Vector2.right, jump = true }, rightEdge, rightInd, Vector2.right*pathfinder.movement.moveSpeed);
                }

                //Jump arcs: Scan all points on surface and project forward (slow!)
                for(int arcStartIndex = 0; arcStartIndex < fromSurf.GetPoints().Count; ++arcStartIndex)
                {
                    Vector2 arcStartPoint = fromSurf.GetPoints()[arcStartIndex];

                    TryAirborneArc(new InputParam { global = Vector2.left , local = Vector2.left , jump = true }, arcStartPoint, arcStartIndex, Vector2.left *pathfinder.movement.moveSpeed);
                    TryAirborneArc(new InputParam { global = Vector2.right, local = Vector2.right, jump = true }, arcStartPoint, arcStartIndex, Vector2.right*pathfinder.movement.moveSpeed);
                }
            }
            return connections;
        }

        public void DebugDraw(float maxAngle)
        {
            foreach (Surface s in surfaces) s.DebugDraw(maxAngle);
        }
    }
}
