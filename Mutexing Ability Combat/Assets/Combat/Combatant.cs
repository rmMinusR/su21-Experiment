using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Combatant : ScopedEventListener, IDamageable, IDamageDealer, IStatusEffectable
{
    #region Health system

    [SerializeField] [InspectorReadOnly(editing = InspectorReadOnlyAttribute.Mode.ReadOnly, playing = InspectorReadOnlyAttribute.Mode.ReadWrite)] protected float health;
    [SerializeField] protected float maxHealth;

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
        EventBus.AddListener(this, typeof(Events. DeathEvent), Events.Priority.Final);
    }

    public override void OnRecieveEvent(Event @event)
    {
        if (!@event.isCancelled)
        {

            if (@event is Events.DamageEvent dmg && dmg.target == this)
            {
                health -= dmg.postMitigation;
                if(!IsAlive()) EventBus.DispatchEvent(new Events.DeathEvent(dmg.source, dmg.target));
            }
            else if (@event is Events.HealEvent heal && heal.target == this)
            {
                health += heal.postMitigation;
            }
            else if (@event is Events.DeathEvent death && death.target == this)
            {
                Destroy(gameObject);
            }

        }
    }

    public abstract bool ShowHealthUI();

    #endregion

    public abstract string GetDisplayName();

    public virtual void Update()
    {
        ShowHealthUI();
        foreach (IStatusEffect effect in statusEffects) effect.OnTick(Time.deltaTime);
    }

    #region Status system

    //FIXME needs custom serialization + editor
    [SerializeField] protected List<IStatusEffect> statusEffects;

    public void ApplyStatus(IStatusEffect effect, IDamageDealer source)
    {
        if(!statusEffects.Contains(effect) && !statusEffects.Any(x => x.GetType() == effect.GetType()))
        {
            if (!EventBus.DispatchEvent(new Events.StatusStartEvent(effect, this)).isCancelled)
            {
                effect.OnStart(this, source);
                statusEffects.Add(effect);
            }
        }
    }

    public void RemoveStatus(IStatusEffect effect)
    {
        if(statusEffects.Contains(effect))
        {
            EventBus.DispatchEvent(new Events.StatusStopEvent(effect, this));
            statusEffects.Remove(effect);
            effect.OnStop();
        }
    }

    public void RemoveAllStatuses()
    {
        foreach (IStatusEffect effect in statusEffects)
        {
            EventBus.DispatchEvent(new Events.StatusStopEvent(effect, this));
            effect.OnStop();
        }

        statusEffects.Clear();
    }


    #endregion
}