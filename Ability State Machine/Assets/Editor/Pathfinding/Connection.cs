using System;
using UnityEngine;

namespace Pathfinding
{
    [Serializable]
    public class Connection
    {
        [SerializeReference] public Node from;
        [SerializeReference] public Node to;

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
