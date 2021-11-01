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
    [SerializeField] private float nextTimeCastable; //Cooldowns
    [SerializeField] private float castStartTime;
    private float castProgress => Mathf.Clamp01( (host.time.stable - castStartTime) / dashDuration );
    [SerializeField] private OwnedMutex<IMovementProvider> ownedMutexMove;

    public override void DoStartCast()
    {
        castStartTime = host.time.stable;

        ownedMutexMove = host.moving.Claim(this);

        host.ui.SetCurrentAbility(null, "Dashing", dashDuration);
    }

    public Vector2 DoMovement(Vector2 velocity, InputParam input) => Vector2.Lerp(input.global * dashSpeedBase * dashSpeedCurve.Evaluate(castProgress), velocity, Mathf.Pow(1-dashLerpAccel, host.time.delta));

    public override bool ShouldEnd() => host.time.stable > dashDuration + castStartTime;

    public override void DoEndCast()
    {
        ownedMutexMove.Release();

        EventBus.Dispatch(new Events.AbilityEndEvent(this, Events.AbilityEndEvent.Reason.CastTimeEnded, true)); //TODO fix
        nextTimeCastable = host.time.stable + cooldown;
    }

    public override void WriteAnimations(PlayerAnimationDriver anim) { }
}
