using System;
using UnityEngine;

public sealed class BaseMovementAction : IAction
{
    public override Vector2 AllowedSimulatedInterval => new Vector2(0, 3);

    public override bool AllowEntry => true;
    public override bool AllowExit => true;

    //Params
    [Header("Movement controls")]
    [Min(0)] public float moveSpeed;

    [Range(0, 1)] public float staticFrictionSnap;
    public float _adjustedCutoffSnappiness => staticFrictionSnap * moveSpeed;

    [Range(0.95f, 1)] public float groundControl;
    [Range(0.80f, 1)] public float airControl;
    private float CurrentControl(float groundedness) => Mathf.Lerp(airControl, groundControl, groundedness);


    public override void DoSetup(MovementController context, IAction prev, bool isSimulated) { }
    public override void DoCleanup(MovementController context, IAction next, bool isSimulated) { }

    public override Vector2 DoPhysics(MovementController context, Vector2 velocity, TimeParam time, InputParam input, float groundedness, bool isSimulated)
    {
        //Apply gravity
        velocity += Physics2D.gravity * time.delta;

        //Get user input
        float localInput = Vector2.Dot(input.global, context.surfaceRight);
        if(!isSimulated) context.facing = FacingExt.Detect(localInput, 0.05f);

        if(!isSimulated) velocity = _DoSurfaceSticking(context, velocity, time.delta);

        //Velocity to local space
        Vector2 localVelocity = isSimulated ? velocity : (Vector2)context.surfaceToGlobal.inverse.MultiplyVector(velocity);

        //Edit surface-relative-X velocity
        localVelocity.x = Mathf.Lerp(localInput, localVelocity.x / moveSpeed, Mathf.Pow(1 -  CurrentControl(groundedness), time.delta)) * moveSpeed;

        //Snappiness thresholding
        if(groundedness > 0.05f) {
            if(Mathf.Abs(localInput) > 0.01f) {
                //Boost
                if (Mathf.Abs(localVelocity.x) < _adjustedCutoffSnappiness && Mathf.Sign(localVelocity.x) == Mathf.Sign(localInput)) localVelocity.x = _adjustedCutoffSnappiness * Mathf.Sign(localVelocity.x);
            } else {
                //Cutoff
                if (Mathf.Abs(localVelocity.x) < _adjustedCutoffSnappiness) localVelocity.x = 0;
            }
        }

        //Transform back to global space
        velocity = isSimulated ? localVelocity : (Vector2)context.surfaceToGlobal.MultiplyVector(localVelocity);

        return velocity;
    }

    Vector2 _DoSurfaceSticking(MovementController context, Vector2 velocity, float deltaTime)
    {
        if(context.lastKnownFlattest.HasValue)
        {
            //If we're grounded and not trying to jump, and the last known flattest contact is considered grabbable (or we're in zero gravity)
            bool grabbable = ClimbOverride.Process(context.IsGrabbable(context.lastKnownFlattest.Value.type), context.lastKnownFlattest.Value.contact.collider.gameObject);
            if (context.IsGrounded && grabbable && context.controlJump.ReadValue<float>() < 0.5f)
            {
                //Slope antislide
                return velocity - Vector2Ext.Proj(Physics2D.gravity * deltaTime, context.surfaceRight);
            }
        
        }
        
        return velocity;
    }
}