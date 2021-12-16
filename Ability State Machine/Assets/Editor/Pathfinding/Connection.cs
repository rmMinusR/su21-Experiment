using System;
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

        public Connection(Node from, Node to, InputParam input)
        {
            this.from = from;
            this.to = to;
            this.input = input;
        }
    }
}
