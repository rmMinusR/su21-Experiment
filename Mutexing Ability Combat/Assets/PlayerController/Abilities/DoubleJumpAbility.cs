using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleJumpAbility : ICastableAbility, IMovementProvider
{
    protected override void DoEventRegistration()
    {
        throw new System.NotImplementedException();
    }

    public override void OnRecieveEvent(Event e)
    {
        throw new System.NotImplementedException();
    }

    public override bool ShouldStart()
    {
        throw new System.NotImplementedException();
    }

    public override void DoStartCast()
    {
        throw new System.NotImplementedException();
    }

    public Vector2 DoMovement(Vector2 currentVelocity, InputParam input)
    {
        throw new System.NotImplementedException();
    }

    public override bool ShouldEnd()
    {
        throw new System.NotImplementedException();
    }

    public override void DoEndCast()
    {
        base.DoEndCast();
    }

    public override void WriteAnimations(PlayerAnimationDriver anim)
    {
        throw new System.NotImplementedException();
    }
}
