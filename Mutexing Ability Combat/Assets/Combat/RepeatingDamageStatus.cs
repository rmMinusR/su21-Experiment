using System.Collections;
using UnityEngine;

public class RepeatingDamageStatus : IStatusEffect, IDamagingEffect
{
    public float damagePerTick;
    public float ticksPerSecond;
    [SerializeField] [InspectorReadOnly] private float timeToNextTick = 0;
    [SerializeField] [InspectorReadOnly] private float timeRemaining;

    public RepeatingDamageStatus(float damagePerTick, float ticksPerSecond, float timeActive)
    {
        this.damagePerTick = damagePerTick;
        this.ticksPerSecond = ticksPerSecond;
        timeRemaining = timeActive;
    }

    public float GetDamage() => throw new System.NotImplementedException();
    public IDamageDealer GetSource() => source;

    public override void OnRecieveEvent(Event e) { }

    public override void OnTick(float deltaTime)
    {
        base.OnTick(deltaTime);

        timeToNextTick -= deltaTime;
        if (timeToNextTick <= 0)
        {
            EventBus.Dispatch(new Events.DamageEvent(source, this, (IDamageable)owner, damagePerTick));
            timeToNextTick = 1 / ticksPerSecond;
        }

        timeRemaining -= deltaTime;
        if (timeRemaining <= 0) owner.RemoveStatus(this);
    }
}
