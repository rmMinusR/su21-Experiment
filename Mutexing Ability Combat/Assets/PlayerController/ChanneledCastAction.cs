using UnityEngine;
using UnityEngine.InputSystem;

public class ChanneledCastAction : MonoBehaviour, IAction
{
    public Vector2 AllowedSimulatedInterval => throw new System.NotImplementedException();

    private InputAction controlActivate;
    private void Awake()
    {
        controlActivate = GetComponent<PlayerHost>().controlsMap.FindAction("Cast");
        Debug.Assert(controlActivate != null);
    }

    [SerializeField] private AnimationClip animCastBegin;
    [SerializeField] private AnimationClip animCastLoop;

    [SerializeField] [Min(0)] private float maxChannelTime = 3.5f;
    [SerializeField] [Min(0)] private float castEndLag = 0.5f;
    [SerializeField] [Min(0)] private float cooldown = 2.0f;

    //TODO fix
    [SerializeField] private float nextTimeCastable;
    [SerializeField] private bool isGood = false;
    [SerializeField] private PlayerUIDriver.AbilityEndReason exitReason;

    public bool AllowEntry(in PlayerHost.Context context) => controlActivate.ReadValue<float>() > 0.5f && nextTimeCastable < context.time.stable;

    public void DoSetup(ref PlayerHost.Context context, IAction prev, IAction.ExecMode mode)
    {
        context.owner.anim.PlayAnimation(animCastBegin, immediately: true);
        context.owner.anim.PlayAnimation(animCastLoop, immediately: false);
        activeUntil = context.time.active + animCastBegin.length;
        context.owner.ui.SetCurrentAbility(null, "Channeled cast", maxChannelTime);
        isGood = true;
        exitReason = PlayerUIDriver.AbilityEndReason.CastTimeEnded;
        nextTimeCastable = context.time.stable + maxChannelTime + cooldown; //Prevent accidental looping
    }

    [Header("For animator")]
    [SerializeField] private Vector2 velocityOverride;
    [SerializeField] [Range(0, 1)] private float overrideSmoothing;

    [Header("State data")]
    //FIXME bad practice, DoPhysics is supposed to be stateless
    [SerializeField] private float activeUntil = 0;

    public Vector2 DoPhysics(ref PlayerHost.Context context, Vector2 velocity, IAction.ExecMode mode)
    {
        //Apply gravity
        velocity += Physics2D.gravity * context.time.delta;

        velocity = Vector2.Lerp(velocityOverride, velocity, Mathf.Pow(overrideSmoothing, context.time.delta));

        if (controlActivate.ReadValue<float>() < 0.5f)
        {
            exitReason = PlayerUIDriver.AbilityEndReason.Cancelled;
            isGood = false;
        }

        if(isGood) activeUntil = Mathf.Min(context.time.active + castEndLag, maxChannelTime);

        return velocity;
    }

    public bool AllowExit(in PlayerHost.Context context) => context.time.active >= activeUntil;

    public void DoCleanup(ref PlayerHost.Context context, IAction next, IAction.ExecMode mode)
    {
        context.owner.ui.ClearCurrentAbility(exitReason);
        nextTimeCastable = context.time.stable + cooldown;
    }
}
