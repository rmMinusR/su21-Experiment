using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class PlatformingProfiler : EditorWindow
{
    [MenuItem("Tools/Platforming Profiler")]
    public static void Open()
    {
        EditorWindow.GetWindow(typeof(PlatformingProfiler));
    }

    private MovementController character;
    private Vector3 startPosition;
    private Vector3 endPosition;

    private void OnSelectionChange()
    {
        //If we don't have a MovementController captured, try to do so on changing selection
        if(Selection.activeGameObject != null)
        {
            MovementController m = Selection.activeGameObject.GetComponent<MovementController>();
            if (character == null && m != null) character = m;
        }
    }

    private void OnFocus()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
        SceneView.duringSceneGui += this.OnSceneGUI;
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Label("Character", EditorStyles.boldLabel);
        
    }

    private void OnSceneGUI(SceneView context)
    {
        //Draw handles and gizmos
        EditorGUI.BeginChangeCheck();
        
        //Draw arrow
        startPosition = Handles.PositionHandle(startPosition, Quaternion.identity);
        endPosition   = Handles.PositionHandle(endPosition, Quaternion.identity);
        Handles.color = Color.Lerp(Color.red, Color.yellow, 0.5f);
        EditorExt.DrawArrow(startPosition, endPosition, Vector3.forward, Handles.DrawLine);

        //Draw raycast down from start pos
        Handles.color = Color.cyan;
        RaycastHit2D rc = Physics2D.Raycast(startPosition, Vector2.down);
        if(rc.collider != null) Handles.DrawDottedLine(startPosition, rc.point, 2);

        if (character != null)
        {
            Handles.color = Color.green;
            List<SimulatedPathData> path = SimulatePath(0.05f);
            {
                List<Vector3> vec3Path = path.ConvertAll(x => (Vector3)x.pos);
                Handles.DrawPolyLine(vec3Path.ToArray());
            }
            //foreach(SimulatedPathData i in path) Handles.Label(i.pos, i.time.ToString());
        }

        EditorGUI.EndChangeCheck();
    }

    private struct SimulatedPathData
    {
        public Vector2 pos;
        public Vector2 vel;
        public float  time;
        public bool grounded;
    }

    private List<SimulatedPathData> SimulatePath(float timeStep)
    {
        List<SimulatedPathData> output = new List<SimulatedPathData>();

        SimulatedPathData data = new SimulatedPathData();
        data.pos = startPosition;
        data.vel = Vector2.zero;
        data.time = 0;

        TimeParam t;
        t.timeActive = 0;
        t.delta = timeStep;

        InputParam input;
        float signBeforeMove;

        IAction a = character.activeMovement != null ? character.activeMovement : character.GetComponent<BaseMovementAction>();
        a.DoSetup(character, null, true);
        
        output.Add(data);
        
        do
        {
            signBeforeMove = Mathf.Sign(endPosition.x - data.pos.x);
            input.local = input.global = ((Vector2)endPosition - data.pos).normalized;
            
            data.time = t.timeActive += t.delta;
            
            data.vel = a.DoPhysics(character, data.vel, t, input, 1, true);
            RaycastHit2D rc = Physics2D.Raycast(data.pos, data.vel.normalized, data.vel.magnitude * t.delta);
            data.grounded = rc.collider != null;
            data.pos = data.grounded ? rc.point : (data.pos + data.vel * t.delta);

            if (data.grounded)
            {
                data.pos += Vector2.up * 0.6f * timeStep;
                data.vel = Vector2Ext.Proj(data.vel, new Vector2(rc.normal.y, -rc.normal.x));
            }

            output.Add(data);
        } while (signBeforeMove == Mathf.Sign(endPosition.x - data.pos.x) && t.timeActive < 10f);

        a.DoCleanup(character, null, true);

        return output;
    }
}
