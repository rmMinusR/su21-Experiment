﻿using System.Collections;
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
}