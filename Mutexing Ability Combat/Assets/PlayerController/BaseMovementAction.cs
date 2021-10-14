using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BaseMovementAction : IAbility
{
    protected override IEnumerator<Type> GetListenedEventTypes() { yield break; }
    public override void OnRecieveEvent(Event e) { }
    
    
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

    public void _ApplyGravity(PlayerHost context, ref Vector2 velocity)
    {
        //Apply gravity
        velocity += Physics2D.gravity * context.time.delta;
    }

    public void _ApplyStaticFriction(PlayerHost context, ref Vector2 velocity, float input)
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

    public Vector2 DoPhysics(PlayerHost context, Vector2 velocity)
    {
        _ApplyGravity(context, ref velocity);

        //Get user input
        //TODO switch to context.input.local?
        float localInput = Vector2.Dot(context.input.global, context.surfaceRight);
        context.facing = FacingExt.Detect(localInput, 0.05f);

        velocity += _ApplySurfaceSticking(context);

        //Velocity to local space
        Vector2 localVelocity = context.surfaceToGlobal.inverse.MultiplyVector(velocity);

        //Edit surface-relative-X velocity
        localVelocity.x = Mathf.Lerp(localInput, localVelocity.x / moveSpeed, Mathf.Pow(1 -  CurrentControl(context.GroundRatio), context.time.delta)) * moveSpeed;

        _ApplyStaticFriction(context, ref localVelocity, localInput);

        //Transform back to global space
        velocity = context.surfaceToGlobal.MultiplyVector(localVelocity);

        //Handle jumping, if applicable
        //TODO only on first press
        if(context.IsGrounded && context.input.jump)
        {
            Vector2 jv = Vector2.Lerp(-Physics2D.gravity.normalized, context.surfaceUp, wallJumpAngle).normalized * jumpForce;
            velocity.y = jv.y;
            velocity.x += jv.x;
            context.MarkUngrounded();
        }

        velocity = _ProcessFakeFriction(velocity);

        //Write for AnimationDriver
        float vx = velocity.x/moveSpeed;
        context.facing = FacingExt.Detect(vx, 0.05f);
        context.anim.PlayAnimation(anims[(int)Mathf.Clamp01(Mathf.Abs(anims.Length*vx))], immediately: true);

        return velocity;
    }

    public Vector2 _ApplySurfaceSticking(PlayerHost context)
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