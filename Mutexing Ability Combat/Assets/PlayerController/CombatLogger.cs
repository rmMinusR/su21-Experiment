using UnityEngine;

public class CombatLogger : ScopedEventListener
{
    protected override void DoEventRegistration()
    {
        EventBus.AddListener(this, typeof(Events.Combat.DamageEvent), Events.Priority.Final);
        EventBus.AddListener(this, typeof(Events.Combat. DeathEvent), Events.Priority.Final);
    }

    public override void OnRecieveEvent(Event e)
    {
        switch(e)
        {
            case Events.Combat.DamageEvent damage: HandleEvent(damage); return;
            case Events.Combat. DeathEvent  death: HandleEvent( death); return;
            default: throw new System.NotImplementedException();
        }
    }

    [Header("Messages")]
    [SerializeField] private TextMessage damageNumberPrefab;
    [SerializeField] private float damageNumberLife;

    protected void HandleEvent(Events.Combat.DamageEvent damage)
    {
        TextMessage obj = Instantiate(damageNumberPrefab.gameObject, ((Component)damage.target).transform.position, Quaternion.identity).GetComponent<TextMessage>();
        obj.uiText.text = damage.postMitigation.ToString();
        obj.lifetime = damageNumberLife;
    }

    protected void HandleEvent(Events.Combat.DeathEvent death)
    {
        TextMessage obj = Instantiate(damageNumberPrefab.gameObject, ((Component)death.target).transform.position, Quaternion.identity).GetComponent<TextMessage>();
        obj.uiText.text = death.target.GetDisplayName() + " killed by " + death.source.GetDisplayName();
        obj.lifetime = damageNumberLife;
    }
}
