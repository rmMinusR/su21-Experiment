using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    [Serializable]
    public class Node
    {
        [SerializeReference] public List<Connection> connections;
        public bool IsFrontier => connections != null;

        public float time;

        //Physics data
        public Vector2 pos;
        public Vector2 vel;

        public enum LedgeType
        {
            Falling = -1,
            None = 0,
            Rising = 1
        }
        public LedgeType ledge;
        public bool grounded;


        public class CompareByTime : IComparer<Node>
        {
            public int Compare(Node x, Node y) => Comparer<float>.Default.Compare(x.time, y.time);
        }
    }
}
