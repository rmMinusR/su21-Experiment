using Events;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Combatant
{
    public override bool ShowHealthUI()
    {
        return false; //TODO
    }

    [Header("Messages")]
    [SerializeField] private TextMessage damageNumberPrefab;
    [SerializeField] private Transform   damageNumberSource;
    [SerializeField] private float       damageNumberLife;

    protected override void HandleEvent(DamageEvent damage)
    {
        if (damage.target == this)
        {
            base.HandleEvent(damage);

            //TODO move to separate script?
            TextMessage obj = Instantiate(damageNumberPrefab.gameObject, damageNumberSource.position, damageNumberSource.rotation).GetComponent<TextMessage>();
            obj.uiText.text = damage.postMitigation.ToString();
            obj.lifetime = damageNumberLife;
        }
    }

    protected override void HandleEvent(DeathEvent death)
    {
        if(death.target == this)
        {
            //TODO move to separate script?
            TextMessage obj = Instantiate(damageNumberPrefab.gameObject, damageNumberSource.position, damageNumberSource.rotation).GetComponent<TextMessage>();
            obj.uiText.text = death.target.GetDisplayName() + " killed by " + death.source.GetDisplayName();
            obj.lifetime = damageNumberLife;
            
            base.HandleEvent(death);
        }
    }

    public override string GetDisplayName() => "Enemy";
}