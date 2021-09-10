using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class AttackAction : IAction
{
    public override Vector2 AllowedSimulatedInterval => new Vector2(0, 5);

    private InputAction controlActivate;
    private void Awake()
    {
        controlActivate = GetComponent<MovementController>().controlsMap.FindAction("Attack");
        Debug.Assert(controlActivate != null);
    }

    public override bool AllowEntry(in MovementController.Context context) => controlActivate.ReadValue<float>() > 0.5f;

    [SerializeField] private AnimationClip[] attackAnims;

    private void _Play(MovementController.Context context, int ind)
    {
        context.owner.animator.Play(attackAnims[ind].name);
        activeUntil = context.time.active + attackAnims[ind].length;
    }

    public override void DoSetup(ref MovementController.Context context, IAction prev, PhysicsMode mode)
    {
        //Play animation
        if (mode == PhysicsMode.Live)
        {
            swingCounter = 0;
            acceptingContinueInput = false;
            _Play(context, 0);
        }
    }

    [Header("For animator")]
    [SerializeField] private Vector2 velocityOverride;
    [SerializeField] [Range(0, 1)] private float overrideSmoothing;
    [SerializeField] private bool acceptingContinueInput;

    [Header("State data")]
    //FIXME bad practice, DoPhysics is supposed to be stateless
    [SerializeField] private float activeUntil = 0;
    [SerializeField] private int swingCounter = 0;

    public override Vector2 DoPhysics(ref MovementController.Context context, Vector2 velocity, PhysicsMode mode)
    {
        Vector2 diff = velocityOverride - velocity;
        velocity += diff * Mathf.Pow(overrideSmoothing, Time.deltaTime);

        if(mode == PhysicsMode.Live)
        {
            //Check if we're allowed to start the next cycle, and if the input says so
            if (acceptingContinueInput && controlActivate.ReadValue<float>() > 0.5f)
            {
                swingCounter = (swingCounter+1) % attackAnims.Length;
                _Play(context, swingCounter);
                acceptingContinueInput = false;
            }
        }

        return velocity;
    }

    public override bool AllowExit(in MovementController.Context context) => context.time.active >= activeUntil;

    public override void DoCleanup(ref MovementController.Context context, IAction next, PhysicsMode mode)
    {
        if (mode == PhysicsMode.Live) context.owner.animator.speed = 1;
        //TODO check not playing? or that exit conditions in animator are met?
    }
}
