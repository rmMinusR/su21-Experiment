using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Hitbox : MonoBehaviour, IDamagingEffect
{
    [SerializeField] private ICombatant owner;
    public ICombatant GetSource() => owner;

    [SerializeField] [Min(0)] private float damage;
    public float GetDamage() => damage;

    private void Awake()
    {
        Debug.Assert(owner != null);

        //Make sure collider state is good
        Debug.Assert(GetComponents<Collider2D>().Any(c => c.isTrigger && c.enabled));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.GetComponent<Hurtbox>() is Hurtbox hurtbox) EventBus.DispatchEvent(new Events.DamageEvent(owner, hurtbox.owner, damage));
    }
}