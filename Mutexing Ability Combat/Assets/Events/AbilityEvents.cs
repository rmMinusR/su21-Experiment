using System;
using UnityEngine;

namespace Events
{
    public abstract class AbilityEvent : Event
    {
        public ICastableAbility ability;

        protected AbilityEvent(ICastableAbility ability)
        {
            this.ability = ability;
        }
    }

    public class AbilityTryCastEvent : AbilityEvent
    {
        public AbilityTryCastEvent(ICastableAbility ability) :
            base(ability)
        {

        }
    }

    public class AbilityStartEvent : AbilityEvent
    {
        public AbilityStartEvent(ICastableAbility ability) :
            base(ability)
        {

        }
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

        public AbilityEndEvent(ICastableAbility ability, Reason reason, bool showMessage = true) :
            base(ability)
        {
            this.reason = reason;
            this.showMessage = showMessage;
        }
    }
}