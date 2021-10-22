using System.Collections;
using System.Linq;
using UnityEngine;

public sealed class ChanneledFireballProjectile : IProjectile, IDamageDealer
{
    [SerializeField] [Min(0)] private float explosionRadius;

    private void Explode()
    {
        float scaledDamage = damage * chargeRatio;

        foreach(IDamageable target in FindObjectsOfType<Component>().Select(t => t as IDamageable).Where(t => t != null))
        {
            GameObject targetGameObject = ((Component)target).gameObject;
            if(Vector2.Distance(targetGameObject.transform.position, transform.position) < explosionRadius)
            {
                EventBus.DispatchEvent(new Events.DamageEvent(this, target, scaledDamage));
            }
        }
    }

    protected override void ProcessEntityCollision(IDamageable target) => Explode();

    protected override void ProcessGroundCollision() => Explode();

    protected override bool ShouldCollide(IDamageable target) => target != source;

    public string GetDisplayName() => source.GetDisplayName() + "'s Fireball";

    public void OnRecieveEvent(Event e) { }
}