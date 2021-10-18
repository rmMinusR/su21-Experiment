public interface IDamagingEffect
{
    public ICombatant GetSource();
    public float GetDamage();
}

public interface IDamageable : IEventListener
{
    internal void _HandleDamageEvent(Events.DamageEvent @event);
}

public interface ICombatant : IDamageable
{
    internal void _HandleHealEvent(Events.HealEvent @event);
}