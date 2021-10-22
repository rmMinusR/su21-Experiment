using System.Collections;
using UnityEngine;

namespace Events
{
    public abstract class CombatEvent : Event
    {
        public IDamageDealer source;
        public IDamageable target;

        public CombatEvent(IDamageDealer source, IDamageable target)
        {
            this.source = source;
            this.target = target;
        }
    }

    public class DamageEvent : CombatEvent
    {
        public readonly float original;
        
        private float? __postAmplification;
        public float postAmplification
        {
            get => __postAmplification ?? original;
            set => __postAmplification = value;
        }

        private float? __postMitigation;
        public float postMitigation
        {
            get => __postMitigation ?? postAmplification;
            set => __postMitigation = value;
        }

        public DamageEvent(IDamageDealer source, IDamageable target, float amount)
            : base(source, target)
        {
            original = amount;
        }
    }

    public class HealEvent : CombatEvent
    {
        public readonly float original;

        private float? __postAmplification;
        public float postAmplification
        {
            get => __postAmplification ?? original;
            set => __postAmplification = value;
        }

        private float? __postReduction;
        public float postMitigation
        {
            get => __postReduction ?? postAmplification;
            set => __postReduction = value;
        }

        public HealEvent(IDamageDealer source, IDamageable target, float amount)
            : base(source, target)
        {
            original = amount;
        }
    }

    public class DeathEvent : CombatEvent
    {
        public DeathEvent(IDamageDealer source, IDamageable target) : base(source, target)
        {
        }
    }




    public abstract class StatusEvent : Event //TODO should this be a CombatEvent instead?
    {
        public IStatusEffect effect;
        public IStatusEffectable target;

        public StatusEvent(IStatusEffect effect, IStatusEffectable target)
        {
            this.effect = effect;
            this.target = target;
        }
    }

    public class StatusStartEvent : StatusEvent
    {
        //TODO add source?

        public StatusStartEvent(IStatusEffect effect, IStatusEffectable target) : base(effect, target)
        {
        }
    }

    public class StatusStopEvent : StatusEvent
    {
        public StatusStopEvent(IStatusEffect effect, IStatusEffectable target) : base(effect, target)
        {
        }
    }
}