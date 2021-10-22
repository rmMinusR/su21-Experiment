using System.Collections;
using UnityEngine;

public class DamageOverTimeStatus : IStatusEffect
{
    public float damagePerSecond;

    public override void OnRecieveEvent(Event e) { }

    public override void OnTick(float deltaTime)
    {
        base.OnTick(deltaTime);
        EventBus.DispatchEvent(new Events.DamageEvent(source, (IDamageable) owner, damagePerSecond * deltaTime));
    }
}
