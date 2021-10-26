using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class ChanneledFireballProjectile : IProjectile, IDamagingEffect
{
    protected override bool ShouldCollide(IDamageable target) => target != source;

    protected override void ProcessEntityCollision(IDamageable target) => Explode();

    protected override void ProcessGroundCollision() => Explode();

    [SerializeField] [Min(0)] private float explosionRadius;
    private void Explode()
    {
        foreach(IDamageable target in FindObjectsOfType<Component>().Select(t => t as IDamageable).Where(t => t != null))
        {
            GameObject targetGameObject = ((Component)target).gameObject;
            if (Vector2.Distance(targetGameObject.transform.position, transform.position) < explosionRadius) Affect(target);
        }
    }

    [Header("Burn DoT")]
    [SerializeField] [Min(0)] float burnDamagePerTick;
    [SerializeField] [Min(0)] float burnTicksPerSecond;
    [SerializeField] [Min(0)] float burnSeconds;
    private void Affect(IDamageable target)
    {
        RepeatingDamageStatus dotStatus = new RepeatingDamageStatus(burnDamagePerTick, burnTicksPerSecond, burnSeconds);

        List<Event> effects = new List<Event> {
            new Events.DamageEvent(source, this, target, GetDamage()),
        };
        if (target is IStatusEffectable statusEffectable) effects.Add(new Events.StatusStartEvent(dotStatus, source, statusEffectable));

        EventBus.DispatchEvent(new Events.SpellAffectEvent(this, source, target, effects));
    }

    public IDamageDealer GetSource() => source;

    public float GetDamage() => damage * chargeRatio;
}