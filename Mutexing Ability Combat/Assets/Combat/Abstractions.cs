using System.Collections.Generic;
using UnityEngine;

public interface IDamagingEffect
{
    public IDamageDealer GetSource();
    public float GetDamage();
}

public interface IDamageable : IEventListener
{
    public bool ShowHealthUI();
    public float GetHealth();
    public float GetMaxHealth();
    public bool IsAlive();
}

public abstract class IDamagingSingleEffector : MonoBehaviour, IDamagingEffect
{
    [SerializeField] protected IDamageDealer owner;
    public IDamageDealer GetSource() => owner;

    public abstract float GetDamage();

    protected virtual void Awake()
    {
        Transform i = transform;
        while (owner == null && i != null) { owner = i.GetComponent<IDamageDealer>(); i = i.parent; }
        Debug.Assert(owner != null);
    }

    [SerializeField] protected List<IDamageable> affected = new List<IDamageable>();
    public void ForgetAffected() => affected.Clear();

    protected void TryAffect(IDamageable target)
    {
        if (!affected.Contains(target))
        {
            ApplyEffects(target);
            affected.Add(target);
        }
    }

    protected virtual void ApplyEffects(IDamageable target) => EventBus.DispatchEvent(new Events.DamageEvent(GetSource(), target, GetDamage()));
}

public interface IDamageDealer : IEventListener
{
    public string GetKillSourceName();
}