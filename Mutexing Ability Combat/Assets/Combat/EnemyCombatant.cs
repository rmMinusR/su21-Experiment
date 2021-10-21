using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCombatant : ScopedEventListener, IDamageable, IDamageDealer
{
    [SerializeField] [InspectorReadOnly(editing = InspectorReadOnlyAttribute.Mode.ReadOnly, playing = InspectorReadOnlyAttribute.Mode.ReadWrite)] private float health;
    [SerializeField] private float maxHealth;

    private void Awake()
    {
        health = maxHealth;
    }

    public float GetHealth() => health;
    public float GetMaxHealth() => maxHealth;
    public bool IsAlive() => health > 0;

    protected override void DoEventRegistration()
    {
        EventBus.AddListener(this, typeof(Events.DamageEvent), Events.Priority.Final);
        EventBus.AddListener(this, typeof(Events.  HealEvent), Events.Priority.Final);
    }

    public override void OnRecieveEvent(Event @event)
    {
        if (@event is Events.DamageEvent dmg && dmg.target == this)
        {
            health -= dmg.postMitigation;
        }
        else if (@event is Events.HealEvent hl && hl.target == this)
        {
            health -= hl.postMitigation;
        }
    }

    public bool ShowHealthUI()
    {
        return false; //TODO
    }

    public string GetKillSourceName() => "Enemy";
}