using System;
using UnityEditor;

[CustomEditor(typeof(BaseMovementAction))]
public class BaseMovementActionEditor : IActionEditor<BaseMovementAction>
{
    protected override void RenderAllGraphs(BaseMovementAction obj)
    {
        PlayerHost host = obj.GetComponent<PlayerHost>();

        RenderGraph("Grounded", base.simulatedInterval, base.simulatedDeltaTime, host, obj,
            (c, v) => {
                c.MarkGrounded();
                return obj.DoPhysics(ref c, v, IAction.ExecMode.SimulateCurves);
            }
        );

        RenderGraph("Airborne", base.simulatedInterval, base.simulatedDeltaTime, host, obj,
            (c, v) => {
                c.MarkUngrounded();
                return obj.DoPhysics(ref c, v, IAction.ExecMode.SimulateCurves);
            }
        );
    }
}