using System;
using System.Collections.Generic;
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


    [Header("Jumping")]
    [SerializeField] [Range(0, 1)] private float wallJumpAngle = 0.5f;
    [SerializeField] [Min(0)]      private float jumpForce;


    public override void DoSetup(ref MovementController.Context context, IAction prev, PhysicsMode mode) { }
    public override void DoCleanup(ref MovementController.Context context, IAction next, PhysicsMode mode) { }

    public override Vector2 DoPhysics(ref MovementController.Context context, Vector2 velocity, PhysicsMode mode)
    {
        //Apply gravity
        velocity += Physics2D.gravity * context.time.delta;

        //Get user input
        float localInput = Vector2.Dot(context.input.global, context.surfaceRight);
        if(mode == PhysicsMode.Live) context.facing = FacingExt.Detect(localInput, 0.05f);

        if(mode != PhysicsMode.SimulateCurves) velocity += _DoSurfaceSticking(context);

        //Velocity to local space
        Vector2 localVelocity = (mode==PhysicsMode.SimulateCurves) ? velocity : (Vector2)context.surfaceToGlobal.inverse.MultiplyVector(velocity);

        //Edit surface-relative-X velocity
        localVelocity.x = Mathf.Lerp(localInput, localVelocity.x / moveSpeed, Mathf.Pow(1 -  CurrentControl(context.GroundRatio), context.time.delta)) * moveSpeed;

        //Snappiness thresholding
        if(context.GroundRatio > 0.05f) {
            if(Mathf.Abs(localInput) > 0.01f) {
                //Boost
                if (Mathf.Abs(localVelocity.x) < _adjustedCutoffSnappiness && Mathf.Sign(localVelocity.x) == Mathf.Sign(localInput)) localVelocity.x = _adjustedCutoffSnappiness * Mathf.Sign(localVelocity.x);
            } else {
                //Cutoff
                if (Mathf.Abs(localVelocity.x) < _adjustedCutoffSnappiness) localVelocity.x = 0;
            }
        }

        //Transform back to global space
        velocity = (mode==PhysicsMode.SimulateCurves) ? localVelocity : (Vector2)context.surfaceToGlobal.MultiplyVector(localVelocity);

        //Handle jumping, if applicable
        //TODO only on first press
        if(context.IsGrounded && context.input.jump)
        {
            Vector2 jv = Vector2.Lerp(-Physics2D.gravity.normalized, context.surfaceUp, wallJumpAngle).normalized * jumpForce;
            velocity.y = jv.y;
            velocity.x += jv.x;
            context.MarkUngrounded();
        }

        if (mode == PhysicsMode.Live) velocity = _ProcessFakeFriction(velocity);

        //Write facing for AnimationDriver
        if (mode == PhysicsMode.Live) context.facing = FacingExt.Detect(context.input.global.x, 0.05f);

        return velocity;
    }

    Vector2 _DoSurfaceSticking(MovementController.Context context)
    {
        if (context.lastKnownFlattest.HasValue)
        {
            //If we're on a surface and not trying to jump
            if (context.IsGrounded && !context.input.jump)
            {
                //Slope antislide
                return - Vector2Ext.Proj(Physics2D.gravity * context.time.delta, context.surfaceRight);
            }
        }

        return Vector2.zero;
    }

    #region Fake friction on contact

    private List<Vector2> fakeFrictionTangents = new List<Vector2>();
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 normal = collision.GetContact(0).normal;
        Vector2 tangent = new Vector2(normal.y, -normal.x);
        fakeFrictionTangents.Add(tangent);
    }

    private Vector2 _ProcessFakeFriction(Vector2 velocity)
    {
        Vector2 @out = velocity;
        foreach (Vector2 v in fakeFrictionTangents) @out = Vector2Ext.Proj(@out, v);
        fakeFrictionTangents.Clear();
        return @out;
    }

    #endregion
}