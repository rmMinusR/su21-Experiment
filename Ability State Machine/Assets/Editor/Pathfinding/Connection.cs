using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Pathfinding
{
    [Serializable]
    public class Connection
    {
        public struct Node
        {
            [SerializeReference] public Surface surface;
            public int index;
            public Vector2 point; //For faster/easier lookup later
        }

        public Node from;
        public Node to;

        //What brings us here?
        public InputParam input;
        public List<PhysicsSimulator.Frame> details;

        public float TimeCost => details!=null ? details[details.Count-1].time : float.PositiveInfinity;

        public Connection(Node from, Node to, InputParam input, List<PhysicsSimulator.Frame> details)
        {
            this.from = from;
            this.to = to;
            this.input = input;
            this.details = details;
        }

        public void DebugDraw()
        {
            //Render basic path
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(3, details.ConvertAll(x => (Vector3)x.pos).ToArray());
            //Handles.DrawAAPolyLine(3, from.point, to.point);

            /*
            //Render time labels
            for (float i = 0; i < details.Count; i += timeLabelResolution / timeResolution) Handles.Label(details[(int)i].pos, details[(int)i].time.ToString("n2") + "s");

            //Render head to show forking
            if (showDebugData)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireCube(details[0].pos, Vector3.one * 0.05f);
            }

            //Render ledge debug data
            if (showDebugData) foreach (SimulatedPathFrame ledge in segment.Where(x => x.ledge != SimulatedPathFrame.LedgeType.None))
            {
                if(ledge.ledge == SimulatedPathFrame.LedgeType.Rising ) Handles.color = Color.cyan;
                if(ledge.ledge == SimulatedPathFrame.LedgeType.Falling) Handles.color = Color.red;
                Handles.DrawWireCube(ledge.pos, Vector3.one * 0.15f);
            }
            */
        }
    }
}
