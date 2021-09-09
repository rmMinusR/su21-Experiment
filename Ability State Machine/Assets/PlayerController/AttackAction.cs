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

    [SerializeField] private AnimationClip attackAnim;

    public override void DoSetup(ref MovementController.Context context, IAction prev, PhysicsMode mode)
    {
        if (mode == PhysicsMode.Live)
        {
            //Play animation
            context.owner.animator.Play(attackAnim.name);
            //Scale to target length
            context.owner.animator.speed = attackAnim.length / durationActive;
        }
    }

    [SerializeField] private AnimationCurve impulseCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] private AnimationCurve dampingCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [SerializeField] [Min(0)] private float durationActive;

    public override Vector2 DoPhysics(ref MovementController.Context context, Vector2 velocity, PhysicsMode mode)
    {
        //Apply damping
        velocity *= Mathf.Pow(dampingCurve.Evaluate(context.time.active/durationActive), context.time.delta);

        //Apply impulse
        velocity.x += Mathf.Sign(velocity.x) * impulseCurve.Evaluate(context.time.active/durationActive) * context.time.delta;
        
        return velocity;
    }

    public override bool AllowExit(in MovementController.Context context) => context.time.active >= durationActive;

    public override void DoCleanup(ref MovementController.Context context, IAction next, PhysicsMode mode)
    {
        context.owner.animator.speed = 1;
        //TODO check not playing? or that exit conditions in animator are met?
    }
}
