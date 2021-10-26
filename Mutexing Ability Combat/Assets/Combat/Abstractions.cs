using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitbox, spell, projectile, attack
/// </summary>
public interface IDamagingEffect
{
    public IDamageDealer GetSource();
    public float GetDamage();
}

/// <summary>
/// Has a health pool: player, enemy, breakable window, tree
/// </summary>
public interface IDamageable : IEventListener
{
    public bool ShowHealthUI();
    public float GetHealth();
    public float GetMaxHealth();
    public bool IsAlive();
    public string GetDisplayName();
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

/// <summary>
/// Player, enemy, dart trap
/// </summary>
public interface IDamageDealer : IEventListener
{
    public string GetDisplayName();
}





public interface IStatusEffectable
{
    public void ApplyStatus(IStatusEffect effect, IDamageDealer source);
    public void RemoveStatus(IStatusEffect statusEffect);
    public void RemoveAllStatuses();
}

[System.Serializable]
public abstract class IStatusEffect : IEventListener
{
    [SerializeField] protected IStatusEffectable owner;
    [SerializeField] protected IDamageDealer source;

    public virtual void OnStart(IStatusEffectable owner, IDamageDealer source)
    {
        this.owner = owner;
        this.source = source;
    }

    public virtual void OnTick(float deltaTime) { }

    public abstract void OnRecieveEvent(Event e);

    public virtual void OnStop() { }
}




[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public abstract class IProjectile : MonoBehaviour
{
    [NonSerialized] public Facing facing;
    [NonSerialized] public float chargeRatio;
    [HideInInspector] public IDamageDealer source;

    [SerializeField] protected float damage;
    [SerializeField] protected float speed;

    [SerializeField] [InspectorReadOnly(playing = InspectorReadOnlyAttribute.Mode.ReadWrite)] protected float curLifetime;
    [SerializeField] [Min(0)] protected float maxLifetime;

    private Rigidbody2D __rb;
    protected Rigidbody2D rb => __rb != null ? __rb : (__rb = GetComponent<Rigidbody2D>());

    protected Vector2 velocity
    {
        get => rb.velocity;
        set => rb.velocity = value;
    }

    protected virtual void Start()
    {
        velocity = ( facing==Facing.Right ? Vector2.right : Vector2.left ) * speed;
        curLifetime = maxLifetime;
    }

    protected virtual void Update()
    {
        curLifetime -= Time.deltaTime;
        if (curLifetime <= 0) OnExpire();
    }

    protected void OnTriggerStay2D(Collider2D other)
    {
        if (other.GetComponent<IDamageable>() is IDamageable target && ShouldCollide(target))
        {
            ProcessEntityCollision(target);
            AfterCollision();
        }
        else if (!other.isTrigger && (other.gameObject.isStatic || other.GetComponent<Rigidbody2D>() == null))
        {
            //FIXME where do we intersect?
            ProcessGroundCollision();
            AfterCollision();
        }
    }

    protected abstract bool ShouldCollide(IDamageable target);
    protected abstract void ProcessEntityCollision(IDamageable target);
    protected abstract void ProcessGroundCollision();

    protected virtual void AfterCollision() => Destroy(gameObject);
    protected virtual void OnExpire() => Destroy(gameObject);
}