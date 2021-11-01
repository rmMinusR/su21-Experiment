using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public abstract class Combatant : ScopedEventListener, IDamageable, IDamageDealer, IStatusEffectable
{
    #region Health system

    [SerializeField] [InspectorReadOnly(editing = InspectorReadOnlyAttribute.Mode.ReadOnly, playing = InspectorReadOnlyAttribute.Mode.ReadWrite)] protected float health;
    [SerializeField] protected float maxHealth;

    protected virtual void Awake()
    {
        health = maxHealth;

        foreach (PlayerHost i in FindObjectsOfType<PlayerHost>()) if (i != this) Physics2D.IgnoreCollision(this.GetComponent<Collider2D>(), i.GetComponent<Collider2D>());
    }

    public float GetHealth() => health;
    public float GetMaxHealth() => maxHealth;
    public bool IsAlive() => health > 0;

    public abstract bool ShowHealthUI();

    protected virtual void HandleEvent(Events.DamageEvent damage)
    {
        if(damage.target == this)
        {
            health -= damage.postMitigation;
            if (!IsAlive()) EventBus.Dispatch(new Events.DeathEvent(damage.source, damage.target));
        }
    }

    protected virtual void HandleEvent(Events.HealEvent heal)
    {
        if (heal.target == this)
        {
            health += heal.postMitigation;
        }
    }

    protected virtual void HandleEvent(Events.DeathEvent death)
    {
        if(death.target == this)
        {
            Destroy(gameObject); //TODO replace with actual death anim, ragdoll, etc
        }
    }

    #endregion

    protected override void DoEventRegistration()
    {
        EventBus.AddListener(this, typeof(Events.     DamageEvent), Events.Priority.Final);
        EventBus.AddListener(this, typeof(Events.       HealEvent), Events.Priority.Final);
        EventBus.AddListener(this, typeof(Events.      DeathEvent), Events.Priority.Final);
        EventBus.AddListener(this, typeof(Events.StatusStartEvent), Events.Priority.Final);
        EventBus.AddListener(this, typeof(Events. StatusStopEvent), Events.Priority.Final);
    }

    public override void OnRecieveEvent(Event @event)
    {
        if (!@event.isCancelled)
        {
                 if (@event is Events.     DamageEvent dmg   ) HandleEvent(dmg   );
            else if (@event is Events.       HealEvent heal  ) HandleEvent(heal  );
            else if (@event is Events.      DeathEvent death ) HandleEvent(death );
            else if (@event is Events.StatusStartEvent sstart) HandleEvent(sstart);
            else if (@event is Events. StatusStopEvent sstop ) HandleEvent(sstop );
        }
    }

    public abstract string GetDisplayName();

    public virtual void Update()
    {
        ShowHealthUI();

        foreach (IStatusEffect effect in statusEffects) effect.OnTick(Time.deltaTime);
    }

    #region Status system

    //FIXME needs custom serialization + editor to handle polymorphism
    [SerializeField] protected List<IStatusEffect> statusEffects = new List<IStatusEffect>();

    private void HandleEvent(Events.StatusStartEvent sstart)
    {
        if(!statusEffects.Contains(sstart.effect))
        {
            //Remove old status, if it exists
            List<IStatusEffect> toRemove = statusEffects.Where(x => x.GetType() == sstart.effect.GetType()).ToList();
            foreach (IStatusEffect x in toRemove)
            {
                statusEffects.Remove(x);
                x.OnStop();
            }

            //Add new status
            sstart.effect.OnStart(this, sstart.source);
            statusEffects.Add(sstart.effect);
        }
    }

    private void HandleEvent(Events.StatusStopEvent sstop)
    {
        if (statusEffects.Contains(sstop.effect))
        {
            statusEffects.Remove(sstop.effect);
            sstop.effect.OnStop();
        }
    }

    public void RemoveAllStatuses()
    {
        foreach (IStatusEffect effect in statusEffects)
        {
            EventBus.Dispatch(new Events.StatusStopEvent(effect, this));
            effect.OnStop();
        }

        statusEffects.Clear();
    }


    #endregion
}