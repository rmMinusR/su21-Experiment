using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PlayerHost : MonoBehaviour
{
    private InputActionMap __controlsMap; public InputActionMap controlsMap => __controlsMap != null ? __controlsMap : (__controlsMap = GetComponent<PlayerInput>().actions.actionMaps[0]);
    [NonSerialized] public InputAction controlMovement;
    [NonSerialized] public InputAction controlJump;

    public bool HasControl => true; //TODO !GetComponent<HealthManager>().IsStunned && !Dialogue.BlockMovement;

    void Awake()
    {
        //Capture controls
        controlMovement = controlsMap.FindAction("Move"); Debug.Assert(controlMovement != null);
        controlJump     = controlsMap.FindAction("Jump"); Debug.Assert(controlJump     != null);
        
        //Ensure we have base movement
        baseMovement = GetComponent<BaseMovementAction>();
        Debug.Assert(baseMovement != null);

        context.owner = this;

        _rb = GetComponent<Rigidbody2D>();

        Debug.Assert(anim != null);
        Debug.Assert(ui != null);
    }

    #region Component references

    private Rigidbody2D _rb;

    public PlayerAnimationDriver anim;

    public PlayerUIDriver ui;

    public BaseMovementAction baseMovement { get; private set; }

    #endregion

    [Serializable]
    public struct Context
    {
        public PlayerHost owner;

        public Context(PlayerHost owner)
        {
            this.owner = owner;
            
            _lastGroundTime = -1000;
            _surfaceUp = Vector2.up;
            _lastKnownFlattest = null;

            input = new InputParam();
            time = new TimeParam();
            facing = Facing.Right;
        }

        //Ground checking
        [SerializeField] private float _lastGroundTime;
        public float GroundRatio => 1 - Mathf.Clamp01((time.stable - _lastGroundTime) / owner.ghostJumpTime);
        public bool IsGrounded => time.stable < _lastGroundTime + owner.ghostJumpTime;
        public bool IsFullyGrounded => time.stable <= owner.ghostJumpTime; //TODO does it need epsilon???
        public void MarkUngrounded() => _lastGroundTime = -1000;
        public void MarkGrounded() => _lastGroundTime = time.stable;

        //Surface-local motion
        [SerializeField] private Vector2 _surfaceUp;
        public Vector2 surfaceUp { get => _surfaceUp; set => _surfaceUp = value; }
        public Vector2 surfaceRight => new Vector2(surfaceUp.y, -surfaceUp.x);
        public Matrix4x4 surfaceToGlobal => new Matrix4x4(surfaceRight, surfaceUp, new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1));


        //Live, un-delayed surface motion
        [NonSerialized] private Contact? _lastKnownFlattest; public Contact? lastKnownFlattest { get => _lastKnownFlattest; set => _lastKnownFlattest = value; }
        public float CurrentSurfaceAngle(float time) => IsGrounded ? lastKnownFlattest?.angle ?? 0 : 0;

        //Params most relevant to basic function
        public InputParam input;
        public TimeParam time;
        public Facing facing;
    }

    public Mutex<IAction> casting = new Mutex<IAction>();
    public Mutex<IAction> moving  = new Mutex<IAction>();

    #region Ground/ceiling checking

    [Header("Ground checking")]
    [SerializeField] [Range(0, 180)]   private float __maxGroundAngle;            public float maxGroundAngle => __maxGroundAngle;
    [SerializeField] [Min(0.01f)]      private float _ghostJumpTime = 0.05f;     public float ghostJumpTime  => _ghostJumpTime;

    [Header("Sloped-surface motion")]
    [SerializeField] [Range(0,1)]      private float _localMotionFalloff = 0.1f; public float localMotionFalloff => _localMotionFalloff;

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
            i.angle = Mathf.Abs(Vector2.Angle(-i.contact.normal, Physics2D.gravity)); //Calculate angle of surface relative to current gravity field
            i.type = ResolveContactType(i.contact, i.angle);

            if (!flattest.HasValue || (ClimbOverride.Process(CanClimb(i), i.contact.collider.gameObject) && i.angle < flattest.Value.angle)) flattest = i;
        }
    }

    [NonSerialized] private Contact? flattest;
    private void _DoGroundCheck()
    {
        if (flattest.HasValue)
        {
            //Do ground check
            if (ClimbOverride.Process(CanClimb(flattest.Value), flattest.Value.contact.collider.gameObject)) context.MarkGrounded();

            //Copy data for normals etc
            context.lastKnownFlattest = flattest.Value;
        }

        //Reset for next frame
        flattest = null;
    }

    public enum ContactType
    {
        GroundClimbable, //Valid surface: Normal ground
        AlwaysClimbable, //Valid surface: Overriden as always climbable
        GroundTooSteep,  //Invalid surface: Too steep
        NeverClimbable,  //Invalid surface: Overridden as never climbable
        Hazard           //Hazards
    }

    public static bool IsGrabbable(ContactType type)
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

    [Serializable]
    public struct Contact
    {
        public ContactPoint2D contact;
        public float angle;
        public ContactType type;
    }

    public ContactType ResolveContactType(ContactPoint2D? contact, float contactAngle)
    {
        //TODO if(contact.collider.GetComponent<Hazard>() != null) return ContactType.Hazard;
        if(contact != null && contact.Value.collider.GetComponent<ClimbOverride>() is ClimbOverride co1 && co1.mode == ClimbOverride.Mode.AlwaysClimbable) return ContactType.AlwaysClimbable;
        if(contact != null && contact.Value.collider.GetComponent<ClimbOverride>() is ClimbOverride co2 && co2.mode == ClimbOverride.Mode.NeverClimbable ) return ContactType.NeverClimbable;
        if(contactAngle > maxGroundAngle) return ContactType.GroundTooSteep;
        else                              return ContactType.GroundClimbable;
    }

    private bool CanClimb(Contact c) => c.angle < maxGroundAngle && IsGrabbable(c.type);

    #endregion

    public Context context;

    void FixedUpdate()
    {
        _DoGroundCheck();

        _UpdateContext();

        _rb.velocity = DoPhysicsUpdate(_rb.velocity, ref context, IAction.ExecMode.Live);
    }

    private void _UpdateContext()
    {
        context.time.delta = Time.fixedDeltaTime;
        context.time.active += context.time.delta;
        context.time.stable += context.time.delta;
        context.input.global = controlMovement.ReadValue<Vector2>();
        context.input.local = context.surfaceToGlobal.inverse.MultiplyVector(context.input.global);
        context.input.jump = controlJump.ReadValue<float>() > 0.5f;
    }

    public Vector2 DoPhysicsUpdate(Vector2 velocity, ref Context context, IAction.ExecMode mode)
    {
        //Update local up axis
        context.surfaceUp = Vector3.Slerp(
                context.surfaceUp,
                Vector3.Slerp(-Physics2D.gravity, context.lastKnownFlattest?.contact.normal ?? -Physics2D.gravity, context.GroundRatio),
                1 - Mathf.Pow(1 - localMotionFalloff, context.time.delta)
            ).normalized;

        IAction movementCommand = moving.Owner;
        if (movementCommand == null) movementCommand = baseMovement;
        
        EventBus.Instance.DispatchEvent(PlayerOnPhysicsEvent);

        return velocity;
    }

    private void OnDrawGizmos()
    {
        //Show debug surface lines
        Gizmos.color = Color.red;   Gizmos.DrawLine(transform.position, transform.position + (Vector3)context.surfaceRight);
        Gizmos.color = Color.green; Gizmos.DrawLine(transform.position, transform.position + (Vector3)context.surfaceUp   );
    }
}