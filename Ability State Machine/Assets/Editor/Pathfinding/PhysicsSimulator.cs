using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pathfinding
{
    [Serializable]
    public class PhysicsSimulator
    {
        #region Collider helpers

        public delegate RaycastHit2D CastFunc(Vector2 pos, Vector2 dir);

        public static CastFunc GetCastFunc(GameObject obj)
        {
            foreach (Collider2D coll in obj.GetComponents<Collider2D>())
            {
                if (coll is CapsuleCollider2D cc) return (pos, dir) => Physics2D.CapsuleCast(pos + cc.offset, cc.size * cc.transform.localScale, cc.direction, 0, dir.normalized, dir.magnitude);
                else Debug.LogWarning("Unsupported collider: " + coll.GetType().Name);
            }

            throw new System.NotImplementedException();
        }

        public static Vector2 GetColliderSize(GameObject obj)
        {
            foreach (Collider2D coll in obj.GetComponents<Collider2D>())
            {
                if (coll is CapsuleCollider2D cc) return cc.size * cc.transform.localScale;
                else Debug.LogWarning("Unsupported collider: " + coll.GetType().Name);
            }

            throw new System.NotImplementedException();
        }

        #endregion

        #region Data structures

        public enum LedgeType
        {
            Falling = -1,
            None = 0,
            Rising = 1
        }

        public struct Frame
        {
            public Vector2 pos;
            public Vector2 vel;
            public float time;
            public bool grounded;
            public LedgeType ledge;

            public class CompareByTime : IComparer<Frame>
            {
                public int Compare(Frame x, Frame y)
                {
                    return Comparer<float>.Default.Compare(x.time, y.time);
                }
            }
        }

        #endregion

        #region Settings

        public float physicsEpsilon;
        public float timeResolution;
        public float maxSimulationTime;
        public float ledgeProbeDist;
        public float ledgeDeltaThreshold;

        public void DrawEditorGUI()
        {
            timeResolution      = 1/EditorGUILayout.Slider("Time resolution (FPS)", Mathf.Clamp(1 / timeResolution, 60, 240), 60, 240);
            maxSimulationTime   =  EditorGUILayout.Slider("Max time"              , maxSimulationTime  , 5, 45);
            physicsEpsilon      =  EditorGUILayout.Slider("Epsilon"               , physicsEpsilon     , 0.0001f, 0.002f);
            ledgeProbeDist      =  EditorGUILayout.Slider("Ledge probe distance"  , ledgeProbeDist     , 5, 50);
            ledgeDeltaThreshold =  EditorGUILayout.Slider("Ledge cutoff threshold", ledgeDeltaThreshold, 1, 10);
        }

        #endregion

        #region Simulation

        public void SimulateFrame(PlayerHost character, ref PlayerHost.Context context, ref Frame data)
        {
            CastFunc cast = GetCastFunc(character.gameObject);

            //Tick time and simulate integration
            data.time = context.time.active = context.time.stable += context.time.delta;
            data.vel = character.DoPhysics(data.vel, ref context, IAction.ExecMode.SimulatePath);

            //Simulate collision response
            RaycastHit2D groundCheck = cast(data.pos + Vector2.up*physicsEpsilon, data.vel*timeResolution);
            Vector2 groundTangent = new Vector2(groundCheck.normal.y, -groundCheck.normal.x);
            data.grounded = groundCheck.collider != null;

            //If we hit ground, need to project along it
            if(data.grounded)
            {
                data.pos += groundCheck.fraction * timeResolution * data.vel + groundCheck.normal*physicsEpsilon;
                data.vel = Vector2Ext.Proj(data.vel, groundTangent);
                context.MarkGrounded();
            }
        }

        public void SimulateSegmentForward(PlayerHost character, PlayerHost.Context context, ref List<Frame> path, Func<Frame, Frame, bool> shouldStop, Func<Frame, InputParam> getInput)
        {
            Frame data = path[path.Count-1];

            Vector2 colliderSize = GetColliderSize(character.gameObject);

            context.currentAction.DoSetup(ref context, null, IAction.ExecMode.SimulatePath);
        
            do
            {
                //Run ledge detection
                data.ledge = DetectLedge(context, data, colliderSize, out _, out _);

                //Setup input
                context.input = getInput(data);

                SimulateFrame(character, ref context, ref data);
            
                //Mark frame
                path.Add(data);
            }
            //Run until we run out of simulation time, or we're told to stop
            while (data.time < maxSimulationTime && !shouldStop(path[path.Count - 2], path[path.Count - 1]));

            context.currentAction.DoCleanup(ref context, null, IAction.ExecMode.SimulatePath);
        }

        #endregion

        public LedgeType DetectLedge(PlayerHost.Context context, Frame data, Vector2 colliderSize, out RaycastHit2D ledgeDet0, out RaycastHit2D ledgeDet1)
        {
            Vector2 posPlusVel = data.pos + data.vel * context.time.delta; //Next frame
            ledgeDet0 = Physics2D.Raycast(data.pos, Physics2D.gravity.normalized, ledgeProbeDist+colliderSize.y); //Where we're standing right now
            float here = ledgeDet0.collider != null ? ledgeDet0.distance : float.MaxValue;
            ledgeDet1 = Physics2D.Raycast(posPlusVel, Physics2D.gravity.normalized, ledgeProbeDist+colliderSize.y); //Where we'll be standing once update is done
            float there = ledgeDet1.collider != null ? ledgeDet1.distance : float.MaxValue;
        
            //Run ledge detection
            float dy = here - there; //Inverted because these are technically distance down, not distance up
            if(Mathf.Abs(dy) > ledgeDeltaThreshold) return (dy>0) ? LedgeType.Rising : LedgeType.Falling;
            else return LedgeType.None;
        }

    }
}
