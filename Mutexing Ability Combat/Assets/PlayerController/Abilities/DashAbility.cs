using UnityEngine;
using UnityEngine.InputSystem;

public sealed class DashAbility : ICastableAbility, IMovementProvider
{
    private InputAction controlActivate;
    private void Awake()
    {
        controlActivate = host.controlsMap.FindAction("Dash");
        Debug.Assert(controlActivate != null);
    }

    protected override void DoEventRegistration() { }
    public override void OnRecieveEvent(Event e) { }

    public override bool ShouldStart() => controlActivate.ReadAsButton() && nextTimeCastable < host.time.stable && !host.moving.IsClaimed;

    [SerializeField] [Min(0)] private float dashSpeedBase = 1;
    [SerializeField] private AnimationCurve dashSpeedCurve = AnimationCurve.Constant(0, 1, 1);
    [SerializeField] [Min(0)] private float dashDuration = 1;
    [SerializeField] [Min(0)] private float dashLerpAccel = 0.95f;

    [Space]
    [SerializeField] [Min(0)] private float cooldown = 1;

    [Header("State data")]
    [SerializeField] private Vector2 dashDir;
    [SerializeField] private float nextTimeCastable; //Cooldowns
    [SerializeField] private float castStartTime;
    private float castProgress => Mathf.Clamp01( (host.time.stable - castStartTime) / dashDuration );
    [SerializeField] private OwnedMutex<IMovementProvider> ownedMutexMove;

    public override void DoStartCast()
    {
        ownedMutexMove = host.moving.Claim(this);

        castStartTime = host.time.stable;
        dashDir = Vector2.zero;

        host.ui.SetCurrentAbility(null, "Dashing", dashDuration);
    }

    public Vector2 DoMovement(Vector2 velocity, InputParam input)
    {
        dashDir = Vector2.Lerp(dashDir, input.global, Time.fixedDeltaTime);
        if (Vector2.Dot(velocity, dashDir) < 0) velocity *= 1-Mathf.Pow(1-0.1f, Time.deltaTime);

        velocity = Vector2.Lerp(dashDir.normalized * dashSpeedBase * dashSpeedCurve.Evaluate(castProgress), velocity, Mathf.Pow(1 - dashLerpAccel, host.time.delta));
        
        return velocity;
    }

    public override bool ShouldEnd() => host.time.stable > dashDuration + castStartTime;

    public override void DoEndCast()
    {
        ownedMutexMove.Release();

        EventBus.Dispatch(new Events.AbilityEndEvent(this, Events.AbilityEndEvent.Reason.CastTimeEnded, true));
        nextTimeCastable = host.time.stable + cooldown;
    }

    public override void WriteAnimations(PlayerAnimationDriver anim) { }
}
