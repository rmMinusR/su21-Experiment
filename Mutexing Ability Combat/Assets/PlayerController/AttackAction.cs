using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AttackAction : ICastableAbility
{
    private InputAction controlActivate;
    private void Awake()
    {
        controlActivate = host.controlsMap.FindAction("Attack");
        //TODO bind callback
        Debug.Assert(controlActivate != null);
    }

    protected override IEnumerator<Type> GetListenedEventTypes() { yield break; }
    public override void OnRecieveEvent(Event e) { }

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

    public void DoSetup(PlayerHost context)
    {
        //Play animation
        inputBuffer = false;
        swingCounter = 0;
        _Play(context, 0);
        acceptingInput = false;
        allowTransition = false;
    }

    [Header("For animator")]
    [SerializeField] private Vector2 velocityOverride;
    [SerializeField] [Range(0, 1)] private float overrideSmoothing;
    [SerializeField] private bool acceptingInput = true;
    [SerializeField] private bool allowTransition;

    [Header("State data")]
    [SerializeField] private float activeUntil = 0;
    [SerializeField] private int swingCounter = 0;
    [SerializeField] private bool inputBuffer;

    public bool CanAttack => acceptingInput; //TODO || context.casting.owner != this) && EventBus.Instance.DispatchEvent(new AbilityTryCastEvent());

    public void TryAttack()
    {
        if(CanAttack) inputBuffer = true;
    }

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

    public Vector2 DoPhysics(PlayerHost context, Vector2 velocity)
    {
        //Apply gravity
        velocity += Physics2D.gravity * context.time.delta;

        velocity = Vector2.Lerp(velocityOverride, velocity, Mathf.Pow(overrideSmoothing, context.time.delta));

        ProcessBufferedInput(context);

        return velocity;
    }

    public bool AllowExit(in PlayerHost context) => context.time.stable >= activeUntil;

    public void DoCleanup(PlayerHost context)
    {
        acceptingInput = true;

        //TODO check not playing? or that exit conditions in animator are met?
    }
}
