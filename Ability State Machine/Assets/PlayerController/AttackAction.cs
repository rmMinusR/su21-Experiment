using System;
using UnityEngine;

public class AttackAction : IAction
{
    public override Vector2 AllowedSimulatedInterval => new Vector2(0, 5);

    public override bool AllowEntry => throw new NotImplementedException();

    [SerializeField] private string attackAnimName;

    public override void DoSetup(ref MovementController.Context context, IAction prev, PhysicsMode mode)
    {
        if(mode == PhysicsMode.Live) context.owner.animator.Play(attackAnimName);
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

    public override bool AllowExit => throw new NotImplementedException();

    public override void DoCleanup(ref MovementController.Context context, IAction next, PhysicsMode mode)
    {
        //TODO check not playing? or that exit conditions in animator are met?
    }
}
