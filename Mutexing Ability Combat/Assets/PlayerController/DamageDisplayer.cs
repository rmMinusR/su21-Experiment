using UnityEngine;

public sealed class DamageDisplayer : ScopedEventListener
{
    [SerializeField] private Combatant owner;

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

    [Header("Display text")]
    [SerializeField] private TextMessage damageNumberPrefab;
    [SerializeField] private float damageNumberLife;

    private void HandleEvent(Events.Combat.DamageEvent damage)
    {
        if((object)damage.target == owner)
        {
            TextMessage obj = Instantiate(damageNumberPrefab.gameObject, ((Component)damage.target).transform.position, Quaternion.identity).GetComponent<TextMessage>();
            obj.uiText.text = damage.postMitigation.ToString();
            obj.lifetime = damageNumberLife;
        }
    }

    private void HandleEvent(Events.Combat.DeathEvent death)
    {
        if ((object)death.target == owner)
        {
            TextMessage obj = Instantiate(damageNumberPrefab.gameObject, ((Component)death.target).transform.position, Quaternion.identity).GetComponent<TextMessage>();
            obj.uiText.text = death.target.GetDisplayName() + " killed by " + death.source.GetDisplayName();
            obj.lifetime = damageNumberLife;
        }
    }
}
