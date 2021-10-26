using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DashAbility : IAbility, IMovementProvider
{
    protected override void DoEventRegistration() { }

    public Vector2 DoMovement(Vector2 currentVelocity, InputParam input)
    {
        throw new System.NotImplementedException();
    }

    public override void OnRecieveEvent(Event e)
    {
        throw new System.NotImplementedException();
    }

    public override void WriteAnimations(PlayerAnimationDriver anim)
    {
        throw new System.NotImplementedException();
    }
}
