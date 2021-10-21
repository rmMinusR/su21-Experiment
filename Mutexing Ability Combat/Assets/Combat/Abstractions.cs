public interface IDamagingEffect
{
    public ICombatant GetSource();
    public float GetDamage();
}

public interface IDamageable : IEventListener
{
    public bool ShowHealthUI();
    public float GetHealth();
    public float GetMaxHealth();
    public bool IsAlive();
}

public interface ICombatant : IDamageable
{
    
}