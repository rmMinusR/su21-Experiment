using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BaseMovementAction : MonoBehaviour, IAction
{
    public Vector2 AllowedSimulatedInterval => new Vector2(0, 3);

    public bool AllowEntry(in PlayerHost.Context context) => false; //Prevent accidentally entering as activeMovementAction
    public bool AllowExit(in PlayerHost.Context context) => true;

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

    [Header("Animations")]
    [SerializeField] private AnimationClip[] anims;

    public void DoSetup(ref PlayerHost.Context context, IAction prev, IAction.ExecMode mode) { }
    public void DoCleanup(ref PlayerHost.Context context, IAction next, IAction.ExecMode mode) { }

    public void _ApplyGravity(PlayerHost.Context context, ref Vector2 velocity, IAction.ExecMode mode)
    {
        //Apply gravity relative to ground, ensuring we don't slide
        Vector2 groundRelativeGravity = Physics2D.gravity;
        if(mode != IAction.ExecMode.SimulateCurves) groundRelativeGravity = Vector2Ext.Proj(groundRelativeGravity, context.groundNormal);
        Debug.DrawLine(context.owner.transform.position, context.owner.transform.position + (Vector3)groundRelativeGravity, Color.cyan);
        velocity += groundRelativeGravity * context.time.delta;
    }

    public void _ApplyStaticFriction(PlayerHost.Context context, ref Vector2 velocity, float input)
    {
        //Snappiness thresholding
        if(context.GroundRatio > 0.05f) {
            if(Mathf.Abs(input) > 0.01f) {
                //Boost
                if (Mathf.Abs(velocity.x) < _adjustedCutoffSnappiness && Mathf.Sign(velocity.x) == Mathf.Sign(input)) velocity.x = _adjustedCutoffSnappiness * Mathf.Sign(velocity.x);
            } else {
                //Cutoff
                if (Mathf.Abs(velocity.x) < _adjustedCutoffSnappiness) velocity.x = 0;
            }
        }
    }

    public Vector2 DoPhysics(ref PlayerHost.Context context, Vector2 velocity, IAction.ExecMode mode)
    {
        _ApplyGravity(context, ref velocity, mode);

        //Get user input
        if(mode == IAction.ExecMode.Live) context.facing = FacingExt.Detect(context.input.local.x, 0.05f);
        
        //*
        //Velocity to local space
        Vector2 localVelocity;
        if (mode == IAction.ExecMode.SimulateCurves) localVelocity = velocity;
        else localVelocity = context.surfaceToGlobal.inverse.MultiplyVector(velocity);

        //Edit surface-relative-X velocity
        localVelocity.x = Mathf.Lerp(context.input.local.x, localVelocity.x / moveSpeed, Mathf.Pow(1 -  CurrentControl(context.GroundRatio), context.time.delta)) * moveSpeed;

        _ApplyStaticFriction(context, ref localVelocity, context.input.local.x);

        //Transform back to global space
        if (mode <= IAction.ExecMode.LiveDelegated) velocity = localVelocity;
        else velocity = context.surfaceToGlobal.MultiplyVector(localVelocity);
        // */

        //Fix collisions being weird
        if (mode <= IAction.ExecMode.LiveDelegated) velocity = _ProcessFakeFriction(velocity);

        //Handle jumping, if applicable
        if (context.IsGrounded && context.input.jump)
        {
            Vector2 jv = Vector2.Lerp(-Physics2D.gravity.normalized, context.groundNormal, wallJumpAngle).normalized * jumpForce;
            velocity.y = jv.y;
            velocity.x += jv.x;
            context.MarkUngrounded();
        }

        //Write for AnimationDriver
        if (mode == IAction.ExecMode.Live)
        {
            float vx = velocity.x/moveSpeed;
            context.facing = FacingExt.Detect(vx, 0.05f);
            context.owner.anim.PlayAnimation(anims[(int)Mathf.Clamp01(Mathf.Abs(anims.Length*vx))], immediately: true);
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