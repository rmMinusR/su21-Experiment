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

    public override void OnRecieveEvent(Event @event)
    {
        base.OnRecieveEvent(@event);

        if (!@event.isCancelled)
        {

            if (@event is Events.DamageEvent dmg)
            {
                TextMessage obj = Instantiate(damageNumberPrefab.gameObject, damageNumberSource.position, damageNumberSource.rotation).GetComponent<TextMessage>();
                obj.uiText.text = dmg.postMitigation.ToString();
                obj.lifetime = damageNumberLife;
            }
            else if (@event is Events.DeathEvent death)
            {
                TextMessage obj = Instantiate(damageNumberPrefab.gameObject, damageNumberSource.position, damageNumberSource.rotation).GetComponent<TextMessage>();
                obj.uiText.text = death.target.GetDisplayName() + " killed by " + death.source.GetDisplayName();
                obj.lifetime = damageNumberLife;
            }

        }
    }

    public override string GetDisplayName() => "Enemy";
}