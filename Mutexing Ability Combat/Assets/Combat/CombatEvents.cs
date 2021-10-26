using System.Collections;
using System.Collections.Generic;
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

    public class SpellAffectEvent : CombatEvent, IEventListener
    {
        public IDamagingEffect spell;
        public List<Event> effects;

        public SpellAffectEvent(IDamagingEffect spell, IDamageDealer source, IDamageable target, List<Event> effects) : base(source, target)
        {
            this.spell = spell;
            this.effects = effects;
        }

        public override void OnPreDispatch() => EventBus.AddListener(this, typeof(SpellAffectEvent), Priority.Final);

        public void OnRecieveEvent(Event e)
        {
            if(e == this)
            {
                ApplyEffects();
            }
        }

        public override void OnPostDispatch() => EventBus.RemoveListener(this, typeof(SpellAffectEvent));

        private void ApplyEffects()
        {
            foreach (Event e in effects) EventBus.Dispatch(e);
        }
    }

    public class DamageEvent : CombatEvent
    {
        public readonly float original;
        public IDamagingEffect effect;

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

        public DamageEvent(IDamageDealer source, IDamagingEffect effect, IDamageable target, float amount)
            : base(source, target)
        {
            this.effect = effect;
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
        public IDamageDealer source;

        public StatusStartEvent(IStatusEffect effect, IDamageDealer source, IStatusEffectable target) : base(effect, target)
        {
            this.source = source;
        }
    }

    public class StatusStopEvent : StatusEvent
    {
        public StatusStopEvent(IStatusEffect effect, IStatusEffectable target) : base(effect, target)
        {
        }
    }
}