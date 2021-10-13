using System;
using UnityEngine;

namespace Events
{
    public sealed class KinematicsEvent : Event
    {
        public readonly Vector2 from;
        public readonly Vector2 velocity;

        public KinematicsEvent(Vector2 from, Vector2 velocity)
        {
            this.from = from;
            this.velocity = velocity;
        }
    }

    public abstract class IntentionalMoveEvent : Event { }

    public sealed class MoveQueryEvent : IntentionalMoveEvent
    {
        public readonly PlayerHost host;
        public Vector2 velocity;

        public MoveQueryEvent(PlayerHost host, Vector2 velocity)
        {
            this.host = host;
            this.velocity = velocity;
        }
    }
}
