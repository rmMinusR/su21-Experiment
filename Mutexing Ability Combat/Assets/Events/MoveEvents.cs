using System;
using UnityEngine;

namespace Events
{
    public abstract class IntentionalMoveEvent : Event { }

    public class BasicMoveEvent : IntentionalMoveEvent
    {
        //Nothing to see here. Just here for the cancellable.
    }

    public class MoveQueryEvent : IntentionalMoveEvent
    {
        public Vector2 movement = Vector2.zero;
        public PlayerHost host;

        public MoveQueryEvent(PlayerHost host)
        {
            this.host = host;
        }
    }
}
