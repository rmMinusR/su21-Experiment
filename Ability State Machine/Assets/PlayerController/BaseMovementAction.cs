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


    public override void DoSetup(MovementController.Context context, IAction prev, PhysicsMode mode) { }
    public override void DoCleanup(MovementController.Context context, IAction next, PhysicsMode mode) { }

    public override Vector2 DoPhysics(MovementController.Context context, Vector2 velocity, PhysicsMode mode)
    {
        //Apply gravity
        velocity += Physics2D.gravity * context.time.delta;

        //Get user input
        float localInput = Vector2.Dot(context.input.global, context.surfaceRight);
        if(mode == PhysicsMode.Live) context.facing = FacingExt.Detect(localInput, 0.05f);

        if(mode != PhysicsMode.SimulateCurves) velocity = _DoSurfaceSticking(context, velocity, context.time.delta);

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
            velocity += Vector2.Lerp(-Physics2D.gravity.normalized, context.surfaceUp, wallJumpAngle).normalized * jumpForce;
            context.MarkUngrounded();
        }

        velocity = _ProcessFakeFriction(velocity);

        return velocity;
    }

    Vector2 _DoSurfaceSticking(MovementController.Context context, Vector2 velocity, float deltaTime)
    {
        if (context.lastKnownFlattest.HasValue)
        {
            //If we're grounded and not trying to jump, and the last known flattest contact is considered grabbable (or we're in zero gravity)
            bool grabbable = ClimbOverride.Process(MovementController.IsGrabbable(context.lastKnownFlattest.Value.type), context.lastKnownFlattest.Value.contact.collider.gameObject);
            if (context.IsGrounded && grabbable && !context.input.jump)
            {
                //Slope antislide
                return velocity - Vector2Ext.Proj(Physics2D.gravity * deltaTime, context.surfaceRight);
            }

        }

        return velocity;
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