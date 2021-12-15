using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    [Serializable]
    public class Graph
    {
        private List<Node> nodes;

        /*
        public IEnumerator<Connection> GetOrCreateConnections(Node node)
        {
            if(node.IsFrontier) return GenerateConnections(node);
            else return node.connections.GetEnumerator();
        }

        private IEnumerator<Connection> GenerateConnections(Node src)
        {
            src.connections = new List<Connection>();


            Connection Connect(InputParam _input)
            {
                Node dst = SimulatePhysicsFrame(src, _input);
                Connection c = new Connection(src, dst, _input);
                src.connections.Add(c);
                return c;
            }

            //Default case: no input
            yield return Connect(new InputParam
            {

            });

            //Need to REGISTER before returning
        }
        */
    }
}