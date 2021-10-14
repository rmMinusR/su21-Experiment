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
    [SerializeField] [Min(0)] private float castEndLag = 0.5f;
    [SerializeField] [Min(0)] private float cooldown = 2.0f;

    //TODO beautify
    [SerializeField] private float nextTimeCastable;
    [SerializeField] private bool isGood = false;
    [SerializeField] private Events.AbilityEndEvent.Reason exitReason;

    public bool AllowEntry(in PlayerHost context) => controlActivate.ReadValue<float>() > 0.5f && nextTimeCastable < context.time.stable;

    public void DoSetup(PlayerHost context)
    {
        context.anim.PlayAnimation(animCastBegin, immediately: true);
        context.anim.PlayAnimation(animCastLoop, immediately: false);
        activeUntil = context.time.stable + animCastBegin.length;
        context.ui.SetCurrentAbility(null, "Channeled cast", maxChannelTime);
        isGood = true;
        exitReason = Events.AbilityEndEvent.Reason.CastTimeEnded;
        nextTimeCastable = context.time.stable + maxChannelTime + cooldown; //Prevent accidental looping
    }

    [Header("For animator")]
    [SerializeField] private Vector2 velocityOverride;
    [SerializeField] [Range(0, 1)] private float overrideSmoothing;

    [Header("State data")]
    //FIXME bad practice, DoPhysics is supposed to be stateless
    [SerializeField] private float activeUntil = 0;

    public Vector2 DoPhysics(PlayerHost context, Vector2 velocity)
    {
        //Apply gravity
        velocity += Physics2D.gravity * context.time.delta;

        velocity = Vector2.Lerp(velocityOverride, velocity, Mathf.Pow(overrideSmoothing, context.time.delta));

        if (controlActivate.ReadValue<float>() < 0.5f)
        {
            exitReason = Events.AbilityEndEvent.Reason.Cancelled;
            isGood = false;
        }

        if(isGood) activeUntil = Mathf.Min(context.time.stable + castEndLag, maxChannelTime);

        return velocity;
    }

    public bool AllowExit(in PlayerHost context) => context.time.stable >= activeUntil;

    public void DoCleanup(PlayerHost context)
    {
        //context.ui.ClearCurrentAbility(exitReason);
        EventBus.Instance.DispatchEvent(new Events.AbilityEndEvent(exitReason));
        nextTimeCastable = context.time.stable + cooldown;
    }
}
