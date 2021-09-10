using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class AttackAction : MonoBehaviour, IAction
{
    public Vector2 AllowedSimulatedInterval => new Vector2(0, 5);

    private InputAction controlActivate;
    private void Awake()
    {
        controlActivate = GetComponent<MovementController>().controlsMap.FindAction("Attack");
        Debug.Assert(controlActivate != null);
    }


    [SerializeField] private AnimationClip[] attackAnims;

    private void _Play(MovementController.Context context, int ind)
    {
        context.owner.animator.Play(attackAnims[ind].name);
        activeUntil = context.time.active + attackAnims[ind].length;
    }

    public bool AllowEntry(in MovementController.Context context) => controlActivate.ReadValue<float>() > 0.5f;

    public void DoSetup(ref MovementController.Context context, IAction prev, IAction.ExecMode mode)
    {
        //Play animation
        if (mode == IAction.ExecMode.Live)
        {
            swingCounter = 0;
            acceptingContinueInput = false;
            _Play(context, 0);
        }
    }

    [Header("For animator")]
    [SerializeField] private Vector2 velocityOverride;
    [SerializeField] [Range(0, 1)] private float overrideSmoothing;
    [SerializeField] private bool acceptingContinueInput;

    [Header("State data")]
    //FIXME bad practice, DoPhysics is supposed to be stateless
    [SerializeField] private float activeUntil = 0;
    [SerializeField] private int swingCounter = 0;

    private void TryStartNextSwing(MovementController.Context context)
    {
        if(acceptingContinueInput)
        {
            swingCounter = (swingCounter + 1) % attackAnims.Length;
            _Play(context, swingCounter);
            acceptingContinueInput = false;
        }
    }

    public Vector2 DoPhysics(ref MovementController.Context context, Vector2 velocity, IAction.ExecMode mode)
    {
        Vector2 diff = velocityOverride - velocity;
        velocity += diff * Mathf.Pow(overrideSmoothing, Time.deltaTime);

        return velocity;
    }

    public bool AllowExit(in MovementController.Context context) => context.time.active >= activeUntil;

    public void DoCleanup(ref MovementController.Context context, IAction next, IAction.ExecMode mode)
    {
        if (mode == IAction.ExecMode.Live) context.owner.animator.speed = 1;
        //TODO check not playing? or that exit conditions in animator are met?
    }
}
