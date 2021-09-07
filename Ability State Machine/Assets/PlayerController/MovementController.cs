using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class MovementController : MonoBehaviour
{
    [NonSerialized] public InputActionMap controlsMap;
    [NonSerialized] public InputAction controlMovement;
    [NonSerialized] public InputAction controlJump;

    public bool HasControl => true; //TODO !GetComponent<HealthManager>().IsStunned && !Dialogue.BlockMovement;

    void Awake()
    {
        //Capture controls
        controlsMap = GetComponent<PlayerInput>().actions.actionMaps[0];
        controlMovement = controlsMap.Where(x => x.name == "Move").First();
        controlJump     = controlsMap.Where(x => x.name == "Jump").First();

        _rb = GetComponent<Rigidbody2D>();

        //Ensure we have base movement
        baseMovement = GetComponent<BaseMovementAction>();
        Debug.Assert(baseMovement != null);
    }

    #region Memoized component references

    private Rigidbody2D _rb;

    public BaseMovementAction baseMovement { get; private set; }

    #endregion

    #region Ground/ceiling checking

    [Header("Ground checking")]
    [SerializeField] [Range(0, 180)]   private float maxGroundAngle;
    [SerializeField] [Min(0.01f)]      private float ghostJumpTime = 0.01f;
    [SerializeField] [HideInInspector] private float _lastGroundTime = -1000;

    public Vector2 surfaceRight => new Vector2(surfaceUp.y, -surfaceUp.x);
    [HideInInspector] public Vector2 surfaceUp = Vector2.up; //Lerps asymptotically like horiz. motion
    public Matrix4x4 surfaceToGlobal => new Matrix4x4(surfaceRight, surfaceUp, new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1));

    public float groundedness => 1 - Mathf.Clamp01((Time.time - _lastGroundTime) / ghostJumpTime);
    public bool IsGrounded => groundedness > 0.05f;
    public bool IsFullyGrounded => groundedness > 0.95f;

    [Header("Sloped-surface motion")]
    [SerializeField] [Range(0,1)] private float localMotionFalloff = 0.1f;

    public float currentSurfaceAngle => IsGrounded ? lastKnownFlattest?.angle ?? 0 : 0;
    [NonSerialized] public Contact? lastKnownFlattest = null;
    private Contact? _flattest = null;
    private int _tmpNContacts = 0; //Count of VALID entries in @_frameContacts
    private ContactPoint2D[] _tmpContactStorage = new ContactPoint2D[0]; //Temporary variable, don't use

    private void OnCollisionStay2D(Collision2D other)
    {
        //Works by finding the lowest angle relative to the current gravity field

        //Resize contact point array only if needed
        if (other.contactCount > _tmpContactStorage.Length) _tmpContactStorage = new ContactPoint2D[other.contactCount];

        //Fetch contact points
        _tmpNContacts = other.GetContacts(_tmpContactStorage);

        //Select best value
        for (int n = 0; n < _tmpNContacts; n++)
        {
            Contact i;
            i.contact = _tmpContactStorage[n];
            i.angle = Vector2.Angle(-i.contact.normal, Physics2D.gravity); //Calculate angle of surface relative to current gravity field
            i.type = ResolveContactType(i.contact, i.angle);

            if (!_flattest.HasValue || ClimbOverride.Process(i.angle < _flattest.Value.angle && _flattest?.type.CompareTo(i.type) <= 0, i.contact.collider.gameObject)) {
                _flattest = i;
            }
        }
    }

    private void _DoGroundCheck()
    {
        if (_flattest.HasValue)
        {
            //Do ground check
            if (ClimbOverride.Process(_flattest.Value.angle < maxGroundAngle, _flattest.Value.contact.collider.gameObject)) _lastGroundTime = Time.time;

            //Copy data for normals etc
            lastKnownFlattest = _flattest.Value;
        }

        //Reset for next frame
        _flattest = null;
    }

    public enum ContactType
    {
        GroundClimbable, //Valid surface: Normal ground
        AlwaysClimbable, //Valid surface: Overriden as always climbable
        GroundTooSteep,  //Invalid surface: Too steep
        NeverClimbable,  //Invalid surface: Overridden as never climbable
        Hazard           //Hazards
    }

    [Serializable]
    public struct Contact
    {
        public ContactPoint2D contact;
        public float angle;
        public ContactType type;
    }

    private ContactType ResolveContactType(ContactPoint2D contact, float contactAngle)
    {
        //TODO if(contact.collider.GetComponent<Hazard>() != null) return ContactType.Hazard;
        if(contact.collider.GetComponent<ClimbOverride>() is ClimbOverride co1 && co1.mode == ClimbOverride.Mode.AlwaysClimbable) return ContactType.AlwaysClimbable;
        if(contact.collider.GetComponent<ClimbOverride>() is ClimbOverride co2 && co2.mode == ClimbOverride.Mode.NeverClimbable ) return ContactType.NeverClimbable;
        if(contactAngle > maxGroundAngle) return ContactType.GroundTooSteep;
        else                              return ContactType.GroundClimbable;
    }

    public bool IsGrabbable(ContactType type)
    {
        switch (type)
        {
            case ContactType.GroundClimbable: return true;
            case ContactType.AlwaysClimbable: return true;
            case ContactType.GroundTooSteep:  return false;
            case ContactType.NeverClimbable:  return false;
            case ContactType.Hazard:          return false;
            default: throw new NotImplementedException();
        } 
    }

    #endregion

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
        foreach(Vector2 v in fakeFrictionTangents) @out = Vector2Ext.Proj(@out, v);
        fakeFrictionTangents.Clear();
        return @out;
    }

    #endregion

    #region Jumping

    [Header("Jumping")]
    [SerializeField] [Range(0, 1)] private float wallJumpAngle = 0.5f;
    [SerializeField] [Min(0)]      private float jumpForce;

    private bool _markShouldJump = false;

    //To be called from Input component
    public void OnJump()
    {
        if (IsGrounded && HasControl) _markShouldJump = true;
    }

    //To be called in physics code
    private Vector2 _GetJumpDV()
    {
        Vector2 val = Vector2.zero;

        if (_markShouldJump && IsGrounded)
        {
            val = Vector2.Lerp(-Physics2D.gravity.normalized, surfaceUp, wallJumpAngle).normalized * jumpForce;
        }

        _markShouldJump = false;
        
        return val;
    }

    #endregion

    void FixedUpdate()
    {
        _DoGroundCheck();

        _DoPhysicsUpdate();
    }

    void _DoPhysicsUpdate()
    {
        //Update local up axis
        surfaceUp = Vector3.Slerp(
                surfaceUp,
                Vector3.Slerp(-Physics2D.gravity, lastKnownFlattest?.contact.normal ?? -Physics2D.gravity, groundedness),
                1 - Mathf.Pow(1 - localMotionFalloff, Time.fixedDeltaTime)
            ).normalized;

        Vector2 velocity = _rb.velocity;

        //Show debug surface lines
        Debug.DrawLine(transform.position, transform.position + (Vector3)surfaceRight, Color.red, 0.2f);
        Debug.DrawLine(transform.position, transform.position + (Vector3)surfaceUp, Color.green, 0.2f);

        //Apply gravity
        //velocity += Physics2D.gravity * Time.fixedDeltaTime;

        //Update params
        activeMovementTime.timeActive += (activeMovementTime.delta = Time.fixedDeltaTime);
        input.global = controlMovement.ReadValue<Vector2>();
        input.local = surfaceToGlobal.inverse.MultiplyPoint(input.global);

        //Execute currently-active movement policy
        if(activeMovement != null) velocity = activeMovement.DoPhysics(this, velocity, activeMovementTime, input, groundedness, false);
        else velocity = baseMovement.DoPhysics(this, velocity, activeMovementTime, input, groundedness, false);

        //Apply jumping (if enqueued)
        velocity += _GetJumpDV();

        //Apply anti-slide
        velocity = _ProcessFakeFriction(velocity);

        //Apply velocity
        _rb.velocity = velocity;
    }

    private InputParam input;
    [SerializeField] private TimeParam activeMovementTime;
    [SerializeReference] private IAction __activeMovement = null;
    [HideInInspector] public Facing facing;

    public IAction activeMovement
    {
        get => __activeMovement;
        set
        {
            //Abort if no value would change
            if (value == __activeMovement) return;

            //Send entry/exit messages
            if (__activeMovement != null) __activeMovement.DoCleanup(this, value, false);
            if (value != null) value.DoSetup(this, __activeMovement, false);

            //Change value
            __activeMovement = value;
            activeMovementTime.timeActive = 0;
        }
    }
}