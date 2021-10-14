using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChanneledCastAction : ICastableAbility
{
    private InputAction controlActivate;
    private void Awake()
    {
        controlActivate = GetComponent<PlayerHost>().controlsMap.FindAction("Cast");
        Debug.Assert(controlActivate != null);
    }

    protected override IEnumerator<Type> GetListenedEventTypes() { yield break; }
    public override void OnRecieveEvent(Event e) { }

    [SerializeField] private AnimationClip animCastBegin;
    [SerializeField] private AnimationClip animCastLoop;

    [SerializeField] [Min(0)] private float maxChannelTime = 3.5f;
    [SerializeField] [Min(0)] private float cooldown = 2.0f;

    public override bool ShouldStart() => controlActivate.ReadAsButton() && nextTimeCastable < host.time.stable;

    public override void DoStartCast()
    {
        host.anim.PlayAnimation(animCastBegin, immediately: true);
        host.anim.PlayAnimation(animCastLoop, immediately: false);
        host.ui.SetCurrentAbility(null, "Channeled cast", maxChannelTime);
        exitReason = Events.AbilityEndEvent.Reason.Interrupted;
        isGood = true;
        channelStartTime = host.time.stable;
    }

    [Header("For animator")]
    [SerializeField] private Vector2 velocityOverride;
    [SerializeField] [Range(0, 1)] private float overrideSmoothing;

    [Header("State data")]
    [SerializeField] private float nextTimeCastable; //Cooldowns
    [SerializeField] private float channelStartTime; //Limit channel time
    [SerializeField] private bool isGood = false;
    [SerializeField] private Events.AbilityEndEvent.Reason exitReason;

    public override void DoWhileCasting()
    {
        if (!controlActivate.ReadAsButton())
        {
            exitReason = Events.AbilityEndEvent.Reason.Cancelled;
            isGood = false;
        }
        else if (host.time.stable > maxChannelTime + channelStartTime)
        {
            exitReason = Events.AbilityEndEvent.Reason.CastTimeEnded;
            isGood = false;
        }
    }

    public Vector2 DoPhysics(PlayerHost context, Vector2 velocity) //TODO fixme
    {
        //Apply gravity
        velocity += Physics2D.gravity * context.time.delta;

        velocity = Vector2.Lerp(velocityOverride, velocity, Mathf.Pow(overrideSmoothing, context.time.delta));

        return velocity;
    }

    public override bool ShouldEnd() => !isGood;

    public override void DoEndCast()
    {
        EventBus.Instance.DispatchEvent(new Events.AbilityEndEvent(this, exitReason, true));
        nextTimeCastable = host.time.stable + cooldown;
    }
}
