using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChanneledFireballSpell : ICastableAbility, IMovementProvider
{
    private InputAction controlActivate;
    private void Awake()
    {
        controlActivate = host.controlsMap.FindAction("Cast");
        Debug.Assert(controlActivate != null);
    }

    protected override void DoEventRegistration() { }
    public override void OnRecieveEvent(Event e) { }

    [SerializeField] private AnimationClip animCastBegin;
    [SerializeField] private AnimationClip animCastLoop;

    [SerializeField] [Min(0)] private float maxChannelTime = 3.5f;
    [SerializeField] [Min(0)] private float cooldown = 2.0f;

    [SerializeField] private OwnedMutex<IAbility> ownedMutexCast;
    [SerializeField] private OwnedMutex<IMovementProvider> ownedMutexMove;

    public override bool ShouldStart() => controlActivate.ReadAsButton() && nextTimeCastable < host.time.stable && !host.casting.IsClaimed && !host.moving.IsClaimed;

    public override void DoStartCast()
    {
        exitReason = Events.AbilityEndEvent.Reason.Interrupted;
        isGood = true;
        channelStartTime = host.time.stable;

        ownedMutexCast = host.casting.Claim(this);
        ownedMutexMove = host.moving.Claim(this);

        host.anim.PlayAnimation(animCastBegin, immediately: true);
        host.anim.PlayAnimation(animCastLoop, immediately: false);
        host.ui.SetCurrentAbility(null, "Channeled Fireball", maxChannelTime);
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

    public Vector2 DoMovement(Vector2 velocity, InputParam _)
    {
        /*
        //Apply gravity
        velocity += Physics2D.gravity * host.time.delta;

        velocity = Vector2.Lerp(velocityOverride, velocity, Mathf.Pow(overrideSmoothing, host.time.delta));
        */

        return host.baseMovement.DoMovement(velocity, new InputParam
        {
            global = Vector2.zero,
            local = Vector2.zero,
            jump = false
        });
    }

    public override bool ShouldEnd() => !isGood;

    [SerializeField] private IProjectile fireballPrefab;
    public override void DoEndCast()
    {
        //Update state
        ownedMutexCast.Release();
        ownedMutexMove.Release();

        EventBus.Dispatch(new Events.AbilityEndEvent(this, exitReason, true));
        nextTimeCastable = host.time.stable + cooldown;

        //Spawn fireball
        float castProgress = Mathf.Clamp01( (host.time.stable - channelStartTime) / maxChannelTime );
        IProjectile fireball = Instantiate(fireballPrefab, host.spellcastOrigin.transform.position, Quaternion.identity).GetComponent<IProjectile>();
        fireball.facing = host.anim.currentFacing;
        fireball.chargeRatio = castProgress;
        fireball.source = host;
    }

    public override void WriteAnimations(PlayerAnimationDriver anim)
    {
        //TODO implement
    }
}
