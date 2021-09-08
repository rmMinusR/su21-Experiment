using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

//[CustomEditor(typeof(TFactory))]
public abstract class IActionEditor<TAction> : Editor
                    where TAction : IAction
{
    public float simulatedDeltaTime = 0.05f;
    public float simulatedInterval = 3;
    
    public sealed override void OnInspectorGUI()
    {
        //Render variables as default
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Simulated acceleration curves", EditorStyles.boldLabel);

        RenderAllGraphs((TAction) target);

        //Render simulation controls (non-persistent)
        EditorGUILayout.PrefixLabel(new GUIContent("Interval", "Total time to be simulated, in seconds"));
        simulatedInterval = EditorGUILayout.Slider(simulatedInterval, ((TAction)target).AllowedSimulatedInterval.x, ((TAction)target).AllowedSimulatedInterval.y);
        EditorGUILayout.PrefixLabel(new GUIContent("Timestep", "Time per simulation step, in seconds"));
        simulatedDeltaTime = EditorGUILayout.Slider(simulatedDeltaTime, 0.01f, 0.2f);

        EditorGUILayout.Space();
        if (GUILayout.Button(new GUIContent("Open Profiler"))) PlatformingProfiler.Open();
    }

    protected abstract void RenderAllGraphs(TAction obj);

    protected delegate InputParam InputSimulatorFunc(float time);
    protected delegate Vector2 VelocitySimulatorFunc(MovementController.Context context, Vector2 velocity);
    protected delegate float VelocityLinearizerFunc(Vector2 velocity);

    //Basic version with defaults for shortcuts and simple tests
    protected static void RenderGraph(string graphName, float simulatedInterval, float timestep,
        MovementController host, TAction obj, VelocitySimulatorFunc f)
    {
        MovementController.Context context = new MovementController.Context(host);

        RenderGraph(graphName, context, simulatedInterval, timestep,
            t => new InputParam {
                global = new Vector2((t <= simulatedInterval / 2) ? 1 : 0, 0),
                local  = new Vector2((t <= simulatedInterval / 2) ? 1 : 0, 0),
                jump = false
            },
            () => obj.DoSetup(context, null, IAction.PhysicsMode.SimulateCurves),
            f,
            () => obj.DoCleanup(context, null, IAction.PhysicsMode.SimulateCurves),
            v => v.x
        );
    }

    //Explicit version for specific tests
    protected static void RenderGraph(string graphName, MovementController.Context context, float simulatedInterval, float timestep,
        InputSimulatorFunc input, Action enter, VelocitySimulatorFunc f, Action exit, VelocityLinearizerFunc lin)
    {
        List<Keyframe> data = new List<Keyframe>();
        EditorGUILayout.PrefixLabel(graphName);

        try
        {
            Vector2 v = Vector2.zero;
            TimeParam t;
            t.stable = 0;
            t.active = 0;
            t.delta = timestep;

            //Write keyframe
            data.Add(new Keyframe(t.active, lin(v), 0, 0, 0, 0));

            enter(); //obj.OnPolicyEntry();

            for (t.active = t.stable = timestep; t.stable <= simulatedInterval; t.active = t.stable += timestep)
            {
                //Simulate
                v = f(context, v);

                //Write keyframe
                data.Add(new Keyframe(t.stable, lin(v), 1, 1, 0, 0));
            }

            exit(); //obj.OnPolicyExit();
        }
        catch(Exception e)
        {
            EditorGUILayout.HelpBox("Unexpected error ("+e.GetType().Name+") while graphing! Check the console for more information.", MessageType.Error, true);

            Debug.LogError("An error occured while rendering graph \""+graphName+"\":");
            Debug.LogError(e);
        }

        EditorGUILayout.CurveField(new AnimationCurve(data.ToArray()));
    }
}
