using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class PlatformingProfiler : EditorWindow
{
    #region Support code

    public enum InputTargettingMode
    {
        Direct,
        EightWay,
        OnlyX,
        OnlyY
    }
    const float EIGHT_WAY_THRESHOLD = 0.05f;

    [MenuItem("Tools/Platforming Profiler")]
    public static void Open()
    {
        EditorWindow.GetWindow(typeof(PlatformingProfiler));
    }

    #endregion

    #region Editable members

    private MovementController character = null;

    private Vector2 startPosition = Vector2.left;
    private Vector2 endPosition   = Vector2.right;

    private InputTargettingMode inputTargettingMode = InputTargettingMode.EightWay;

    #endregion

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
        bool markRepaint = false;

        character = (MovementController) EditorGUILayout.ObjectField("Agent", character, typeof(MovementController), true);

        EditorGUILayout.LabelField("Input targetting mode");
        InputTargettingMode tmp = (InputTargettingMode) EditorGUILayout.EnumPopup(inputTargettingMode);
        if (inputTargettingMode != tmp) { inputTargettingMode = tmp; markRepaint = true; }
        
        if (markRepaint) SceneView.RepaintAll();
    }

    private void OnSceneGUI(SceneView context)
    {
        //Draw handles and gizmos
        EditorGUI.BeginChangeCheck();
        
        //Draw arrow
        startPosition = Handles.PositionHandle(startPosition, Quaternion.identity);
        endPosition   = Handles.PositionHandle(endPosition, Quaternion.identity);
        Handles.color = Color.green;
        EditorExt.DrawArrow(startPosition, endPosition, Vector3.forward, (p1, p2) => Handles.DrawAAPolyLine(p1, p2));

        //Draw raycast down from start and end pos
        Handles.color = Color.cyan;
        {
            RaycastHit2D rc = Physics2D.Raycast(startPosition, Vector2.down, 100f);
            if (rc.collider != null) Handles.DrawDottedLine(startPosition, rc.collider != null ? rc.point : (endPosition + Vector2.down * 100f), 2);
        }
        {
            RaycastHit2D rc = Physics2D.Raycast(endPosition, Vector2.down, 100f);
            Handles.DrawDottedLine(endPosition, rc.collider != null ? rc.point : (endPosition + Vector2.down * 100f), 2);
        }

        if (character != null)
        {
            Handles.color = Color.magenta;
            List<SimulatedPathData> path = SimulatePath(0.05f);
            {
                List<Vector3> vec3Path = path.ConvertAll(x => (Vector3)x.pos);
                Handles.DrawAAPolyLine(3, vec3Path.ToArray());
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

        InputParam input;
        float signBeforeMove;

        //Small offset to make sure raycaster actually hits
        float smallSafetyOffset = 0.005f;
        Vector2 makeSafetyOffset(Vector2 v) => v.normalized * -smallSafetyOffset;

        IAction a = character.activeMovement != null ? character.activeMovement : character.GetComponent<BaseMovementAction>();
        a.DoSetup(character, null, true);
        
        output.Add(data);

        do
        {
            //Update escape condition
            signBeforeMove = Mathf.Sign(endPosition.x - data.pos.x);

            //Calculate input
            Vector2 dp = endPosition - data.pos;
            input.local = input.global = inputTargettingMode switch
            {
                InputTargettingMode.Direct   => dp.normalized,
                InputTargettingMode.EightWay => new Vector2((float)FacingExt.Detect(dp.x, EIGHT_WAY_THRESHOLD), (float)FacingExt.Detect(dp.y, EIGHT_WAY_THRESHOLD)),
                InputTargettingMode.OnlyX    => new Vector2((float)FacingExt.Detect(dp.x, EIGHT_WAY_THRESHOLD), 0),
                InputTargettingMode.OnlyY    => new Vector2(0, (float)FacingExt.Detect(dp.y, EIGHT_WAY_THRESHOLD)),
                _ => throw new System.NotImplementedException(),
            };

            //Tick time and velocity
            data.time += timeStep;
            data.vel = a.DoPhysics(character, data.vel, new TimeParam { timeActive = data.time, delta = timeStep }, input, data.grounded ? 1 : 0, true) ;
            
            //Check to see if we would hit anything while moving
            RaycastHit2D groundCheck = Physics2D.Raycast(data.pos + Vector2.up*smallSafetyOffset, data.vel.normalized, data.vel.magnitude * timeStep + smallSafetyOffset);
            Vector2 groundTangent = new Vector2(groundCheck.normal.y, -groundCheck.normal.x);
            data.grounded = groundCheck.collider != null;
            
            //We didn't hit ground = move normally
            if (!data.grounded) data.pos += data.vel * timeStep;
            //We hit ground = need to project along it
            else {
                Vector2 projVel = Vector2Ext.Proj(data.vel, groundTangent);
                RaycastHit2D projectingPositioner = Physics2D.Raycast(data.pos + projVel * timeStep + makeSafetyOffset(data.vel), -groundCheck.normal, Vector2.Dot(data.vel, groundTangent) + smallSafetyOffset);
                data.pos = projectingPositioner.collider != null ? projectingPositioner.point : (data.pos + projVel * timeStep);
                data.vel = projVel;
            }

            //Mark frame
            output.Add(data);
        }
        //Until we reach our destination, or run out of simulation time
        while (signBeforeMove == Mathf.Sign(endPosition.x - data.pos.x) && data.time < 10f);

        a.DoCleanup(character, null, true);

        return output;
    }
}
