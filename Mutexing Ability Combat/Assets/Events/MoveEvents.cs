using System;
using UnityEngine;

namespace Events
{
    public abstract class IntentionalMoveEvent : Event { }

    public class MoveQueryEvent : IntentionalMoveEvent
    {
        public Vector2 velocity = Vector2.zero;
        public PlayerHost host;

        public MoveQueryEvent(PlayerHost host, Vector2 velocity)
        {
            this.host = host;
            this.velocity = velocity;
        }
    }
}
