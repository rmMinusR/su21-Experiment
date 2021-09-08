﻿using System;
using UnityEditor;

[CustomEditor(typeof(BaseMovementAction))]
public class BaseMovementActionEditor : IActionEditor<BaseMovementAction>
{
    protected override void RenderAllGraphs(BaseMovementAction obj)
    {
        MovementController host = obj.GetComponent<MovementController>();

        RenderGraph("Grounded", base.simulatedInterval, base.simulatedDeltaTime, host, obj,
            (c, v) => obj.DoPhysics(c, v, IAction.PhysicsMode.SimulateCurves)
        );

        RenderGraph("Airborne", base.simulatedInterval, base.simulatedDeltaTime, host, obj,
            (c, v) => obj.DoPhysics(c, v, IAction.PhysicsMode.SimulateCurves)
        );
    }
}