using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

//[CustomEditor(typeof(TFactory))]
public abstract class IActionEditor<TAction> : Editor
                    where TAction : UnityEngine.Object, IAction
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
    protected delegate Vector2 VelocitySimulatorFunc(PlayerHost.Context context, Vector2 velocity);
    protected delegate float VelocityLinearizerFunc(Vector2 velocity);

    //Basic version with defaults for shortcuts and simple tests
    protected static void RenderGraph(string graphName, float simulatedInterval, float timestep,
        PlayerHost host, TAction obj, VelocitySimulatorFunc f)
    {
        PlayerHost.Context context = new PlayerHost.Context(host);
        context.time.delta = timestep;

        RenderGraph(graphName, context, simulatedInterval, timestep,
            t => {
                float input;
                     if(t < simulatedInterval*1/3) input = 1; //Forward
                else if(t < simulatedInterval*2/3) input = -1; //Backward
                else                               input = 0; //Stop
                return new InputParam
                {
                    global = new Vector2(input, 0),
                    local = new Vector2(input, 0),
                    jump = false
                };
            },
            () => obj.DoSetup(ref context, null, IAction.ExecMode.SimulateCurves),
            f,
            () => obj.DoCleanup(ref context, null, IAction.ExecMode.SimulateCurves),
            v => v.x
        );
    }

    //Explicit version for specific tests
    protected static void RenderGraph(string graphName, PlayerHost.Context context, float simulatedInterval, float timestep,
        InputSimulatorFunc input, Action enter, VelocitySimulatorFunc f, Action exit, VelocityLinearizerFunc lin)
    {
        List<Keyframe> data = new List<Keyframe>();
        EditorGUILayout.PrefixLabel(graphName);

        try
        {
            Vector2 v = Vector2.zero;

            //Write keyframe
            data.Add(new Keyframe(context.time.stable, lin(v), 0, 0, 0, 0));

            enter(); //obj.OnPolicyEntry();

            for (context.time.active = context.time.stable = timestep; context.time.stable <= simulatedInterval; context.time.active = context.time.stable += context.time.delta)
            {
                context.input = input(context.time.stable);

                //Simulate
                v = f(context, v);

                //Write keyframe
                data.Add(new Keyframe(context.time.stable, lin(v), 1, 1, 0, 0));
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
