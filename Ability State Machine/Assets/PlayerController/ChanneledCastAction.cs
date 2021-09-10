using UnityEngine;

public class ChanneledCastAction : MonoBehaviour, IAction
{
    public Vector2 AllowedSimulatedInterval => throw new System.NotImplementedException();

    public bool AllowEntry(in MovementController.Context context) => false;

    public void DoSetup(ref MovementController.Context context, IAction prev, IAction.ExecMode mode)
    {
        
    }

    public Vector2 DoPhysics(ref MovementController.Context context, Vector2 velocity, IAction.ExecMode mode)
    {
        return velocity;
    }

    public bool AllowExit(in MovementController.Context context) => false;

    public void DoCleanup(ref MovementController.Context context, IAction next, IAction.ExecMode mode)
    {
        
    }
}
