using UnityEngine;
using UnityEngine.InputSystem;

public class AttackAction : ICastableAbility
{
    private InputAction controlActivate;
    private void Awake()
    {
        controlActivate = GetComponent<PlayerHost>().controlsMap.FindAction("Attack");
        //TODO bind callback
        Debug.Assert(controlActivate != null);
    }

    [SerializeField] private AnimationClip[] attackAnimsGrounded;
    [SerializeField] private AnimationClip[] attackAnimsAirborne;

    private void _Play(PlayerHost context, int ind)
    {
        AnimationClip toPlay = context.IsGrounded ? attackAnimsGrounded[ind] : attackAnimsAirborne[ind];
        context.anim.PlayAnimation(toPlay, immediately: true);
        activeUntil = context.time.stable + toPlay.length;
    }

    public bool AllowEntry(in PlayerHost context)
    {
        bool hasInput = inputBuffer;
        inputBuffer = false;
        return hasInput;
    }

    public void DoSetup(PlayerHost context, IAction prev, IAction.ExecMode mode)
    {
        //Play animation
        if (mode == IAction.ExecMode.Live)
        {
            inputBuffer = false;
            swingCounter = 0;
            _Play(context, 0);
            acceptingInput = false;
            allowTransition = false;
        }
    }

    [Header("For animator")]
    [SerializeField] private Vector2 velocityOverride;
    [SerializeField] [Range(0, 1)] private float overrideSmoothing;
    [SerializeField] private bool acceptingInput = true;
    [SerializeField] private bool allowTransition;

    [Header("State data")]
    //FIXME bad practice, class is supposed to be stateless
    [SerializeField] private float activeUntil = 0;
    [SerializeField] private int swingCounter = 0;
    [SerializeField] private bool inputBuffer;

    //To be called from Input component
    //Buffers a button press
    public void OnAttack() { if(acceptingInput) inputBuffer = true; }

    //Read buffered input and act
    private void ProcessBufferedInput(PlayerHost context)
    {
        if(inputBuffer && allowTransition)
        {
            inputBuffer = false;
            swingCounter = (swingCounter + 1) % (context.IsGrounded ? attackAnimsGrounded.Length : attackAnimsAirborne.Length);
            _Play(context, swingCounter);
            acceptingInput = false;
            allowTransition = false;
        }
    }

    public Vector2 DoPhysics(PlayerHost context, Vector2 velocity, IAction.ExecMode mode)
    {
        //Apply gravity
        velocity += Physics2D.gravity * context.time.delta;

        velocity = Vector2.Lerp(velocityOverride, velocity, Mathf.Pow(overrideSmoothing, context.time.delta));

        if (mode == IAction.ExecMode.Live) ProcessBufferedInput(context);

        return velocity;
    }

    public bool AllowExit(in PlayerHost context) => context.time.stable >= activeUntil;

    public void DoCleanup(PlayerHost context, IAction next, IAction.ExecMode mode)
    {
        acceptingInput = true;

        //TODO check not playing? or that exit conditions in animator are met?
    }
}
