using System;
using UnityEngine;

namespace Events
{
    public abstract class AbilityEvent : Event
    {
        public ICastableAbility ability;
    }

    public class AbilityTryCastEvent : AbilityEvent { }

    public class AbilityStartEvent : AbilityEvent
    {
        public Vector2 aimTargetRaw;
    }

    public class AbilityEndEvent : AbilityEvent
    {
        public enum Reason
        {
            CastTimeEnded,
            Cancelled,
            Interrupted
        }

        public Reason reason;
        public bool showMessage;

        public AbilityEndEvent(Reason reason, bool showMessage = true)
        {
            this.reason = reason;
            this.showMessage = showMessage;
        }
    }
}