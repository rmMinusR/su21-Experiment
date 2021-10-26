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
        Debug.Assert(controlActivate != null);

        //Bind callback
        controlActivate.performed += OnAttack;
    }

    private void OnDestroy()
    {
        //Unbind callback
        controlActivate.performed -= OnAttack;
    }

    protected override void DoEventRegistration() { }
    public override void OnRecieveEvent(Event e) { }

    [SerializeField] private AnimationClip[] attackAnimsGrounded;
    [SerializeField] private AnimationClip[] attackAnimsAirborne;

    [Space]
    [SerializeField] private SpellHitbox hitbox;

    private void _Play(PlayerHost context, int ind)
    {
        AnimationClip toPlay = context.IsGrounded ? attackAnimsGrounded[ind] : attackAnimsAirborne[ind];
        context.anim.PlayAnimation(toPlay, immediately: true);
        activeUntil = context.time.stable + toPlay.length;
    }

    [SerializeField] private OwnedMutex<IAbility> ownedMutexCast;

    public override bool ShouldStart()
    {
        bool hasInput = inputBuffer;
        inputBuffer = false;
        return hasInput && !host.casting.IsClaimed;
    }

    public override void DoStartCast()
    {
        ownedMutexCast = host.casting.Claim(this);

        //Play animation
        inputBuffer = false;
        swingCounter = 0;
        _Play(host, 0);
        acceptingInput = false;
        allowTransition = false;
        hitbox.ForgetAffected(); //Reset so we can hit them again
    }

    [Header("For animator")]
    [SerializeField] private bool acceptingInput = true;
    [SerializeField] private bool allowTransition;

    [Header("State data")]
    [SerializeField] [InspectorReadOnly] private float activeUntil = 0;
    [SerializeField] [InspectorReadOnly] private int swingCounter = 0;
    [SerializeField] [InspectorReadOnly(playing = InspectorReadOnlyAttribute.Mode.ReadWrite)] private bool inputBuffer;

    public bool CanAttack => acceptingInput;

    public void OnAttack(InputAction.CallbackContext callbackContext)
    {
        if(CanAttack) inputBuffer = true;
    }

    public override bool ShouldEnd() => host.time.stable >= activeUntil; //TODO check not playing? or that exit conditions in animator are met?

    public override void DoEndCast()
    {
        ownedMutexCast.Release();

        acceptingInput = true;
    }

    //Read buffered input and apply
    public override void WriteAnimations(PlayerAnimationDriver anim)
    {
        if(inputBuffer && allowTransition)
        {
            inputBuffer = false;
            swingCounter = (swingCounter + 1) % (host.IsGrounded ? attackAnimsGrounded.Length : attackAnimsAirborne.Length);
            _Play(host, swingCounter);
            acceptingInput = false;
            allowTransition = false;
            hitbox.ForgetAffected(); //Reset so we can hit them again
        }
    }
}
