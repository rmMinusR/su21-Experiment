using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SpellHitbox : IDamagingSingleEffector
{
    [SerializeField] [Min(0)] private float damage;
    public override float GetDamage() => damage;

    protected override void Awake()
    {
        base.Awake();

        //Make sure collider state is good
        Debug.Assert(GetComponents<Collider2D>().Any(c => c.isTrigger && c.enabled));
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.GetComponent<Hurtbox>() is Hurtbox hurtbox) TryAffect(hurtbox.owner);
    }
}