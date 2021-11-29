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

    public Vector2 _CalcEffectiveGravity(PlayerHost.Context context, IAction.ExecMode mode)
    {
        //Effective gravity is relative to ground, ensuring we don't slide
        Vector2 groundRelativeGravity = Physics2D.gravity;
        if(mode != IAction.ExecMode.SimulateCurves) groundRelativeGravity = Vector2Ext.Proj(groundRelativeGravity, context.groundNormal);
        return groundRelativeGravity;
    }

    public void _ApplyStaticFriction(PlayerHost.Context context, ref float x, float input)
    {
        //Snappiness thresholding
        if(context.GroundRatio > 0.05f) {
            if(Mathf.Abs(input) > 0.01f) {
                //Boost
                if (Mathf.Abs(x) < _adjustedCutoffSnappiness && Mathf.Sign(x) == Mathf.Sign(input)) x = _adjustedCutoffSnappiness * Mathf.Sign(x);
            } else {
                //Cutoff
                if (Mathf.Abs(x) < _adjustedCutoffSnappiness) x = 0;
            }
        }
    }

    public Vector2 DoPhysics(ref PlayerHost.Context context, Vector2 velocity, IAction.ExecMode mode)
    {
        //Fix collisions having too much energy
        if (mode <= IAction.ExecMode.LiveDelegated) velocity = _ProcessCollisionEnergyLoss(velocity);

        //Apply gravity
        Vector2 fGrav = _CalcEffectiveGravity(context, mode);
        velocity += fGrav * context.time.delta;
        if (mode == IAction.ExecMode.Live) Debug.DrawLine(transform.position, transform.position + (Vector3)fGrav, Color.yellow);

        //Get user input
        context.facing = FacingExt.Detect(context.input.local.x, 0.05f);
        
        //*
        //Velocity to local space
        Vector2 localVelocity = context.surfaceToGlobal.inverse.MultiplyVector(velocity);

        //Edit surface-relative-X velocity
        Vector2 dv = -localVelocity;
        localVelocity.x = Mathf.Lerp(context.input.local.x * moveSpeed, localVelocity.x, Mathf.Pow(1 - CurrentControl(context.GroundRatio), context.time.delta));
        dv += localVelocity;
        if(mode == IAction.ExecMode.Live) Debug.DrawLine(transform.position, transform.position + context.surfaceToGlobal.MultiplyVector(dv)/context.time.delta, Color.cyan, 0.2f);

        _ApplyStaticFriction(context, ref localVelocity.x, context.input.local.x);

        //Transform back to global space
        velocity = context.surfaceToGlobal.MultiplyVector(localVelocity);
        // */

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

    #region Lose energy on contact

    private List<Vector2> fakeFrictionTangents = new List<Vector2>();
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector2 normal = collision.GetContact(0).normal;
        Vector2 tangent = new Vector2(normal.y, -normal.x);
        fakeFrictionTangents.Add(tangent);
    }

    private Vector2 _ProcessCollisionEnergyLoss(Vector2 velocity)
    {
        Vector2 @out = velocity;
        foreach (Vector2 v in fakeFrictionTangents) @out = Vector2Ext.Proj(@out, v);
        fakeFrictionTangents.Clear();
        return @out;
    }

    #endregion
}