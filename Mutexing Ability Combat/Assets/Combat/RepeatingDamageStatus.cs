using System.Collections;
using UnityEngine;

public class RepeatingDamageStatus : IStatusEffect
{
    public float damagePerTick;
    public float ticksPerSecond;
    [SerializeField] [InspectorReadOnly] private float timeToNextTick = 0;

    public override void OnRecieveEvent(Event e) { }

    public override void OnTick(float deltaTime)
    {
        base.OnTick(deltaTime);
        timeToNextTick -= deltaTime;
        if (timeToNextTick <= 0)
        {
            EventBus.DispatchEvent(new Events.DamageEvent(source, (IDamageable)owner, damagePerTick));
            timeToNextTick = 1 / ticksPerSecond;
        }
    }
}
