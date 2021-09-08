using System;
using UnityEditor;

[CustomEditor(typeof(BaseMovementAction))]
public class BaseMovementActionEditor : IActionEditor<BaseMovementAction>
{
    protected override void RenderAllGraphs(BaseMovementAction obj)
    {
        MovementController context = obj.GetComponent<MovementController>();

        RenderGraph("Grounded", base.simulatedInterval, base.simulatedDeltaTime, context, obj,
            (v, t, input) => obj.DoPhysics(context, v, t, input, 1, IAction.PhysicsMode.SimulateCurves)
        );

        RenderGraph("Airborne", base.simulatedInterval, base.simulatedDeltaTime, context, obj,
            (v, t, input) => obj.DoPhysics(context, v, t, input, 0, IAction.PhysicsMode.SimulateCurves)
        );
    }
}