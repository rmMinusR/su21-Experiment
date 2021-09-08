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

    [MenuItem("Tools/Platforming Profiler")]
    public static void Open()
    {
        EditorWindow.GetWindow(typeof(PlatformingProfiler));
    }

    private delegate RaycastHit2D CastFunc(Vector2 pos, Vector2 dir);

    private static CastFunc GetCastFunc(GameObject obj)
    {
        foreach (Collider2D coll in obj.GetComponents<Collider2D>())
        {
            if (coll is CapsuleCollider2D cc) return (pos, dir) => Physics2D.CapsuleCast(pos+cc.offset, cc.size, cc.direction, 0, dir.normalized, dir.magnitude);
            else Debug.LogWarning("Unsupported collider: " + coll.GetType().Name);
        }

        throw new System.NotImplementedException();
    }

    #endregion

    #region Setup/cleanup code

    private void TryCaptureMovementController()
    {
        //If we don't have a MovementController captured, try to do so on changing selection
        if(Selection.activeGameObject != null)
        {
            MovementController m = Selection.activeGameObject.GetComponent<MovementController>();
            if (character == null && m != null) character = m;
        }
    }

    private void Awake() => TryCaptureMovementController();

    private void OnFocus()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
        SceneView.duringSceneGui += this.OnSceneGUI;

        TryCaptureMovementController();
    }

    private void OnDestroy()
    {
        SceneView.duringSceneGui -= this.OnSceneGUI;
    }

    #endregion

    #region Editable members

    private MovementController character = null;

    private Vector2 startPosition = Vector2.left;
    private Vector2 endPosition   = Vector2.right;
    private float maxEndTime = 10f;

    private float snapInputThreshold = 0.05f;

    private float timeResolution = 0.01f;
    private float timeLabelResolution = 0.5f;
    private float physicsEpsilon = 0.0015f;
    private int nMicroframes = 1;

    private InputTargettingMode inputTargettingMode = InputTargettingMode.EightWay;

    #endregion

    private void OnGUI()
    {
        bool markRepaint = false;

        character = (MovementController) EditorGUILayout.ObjectField("Agent", character, typeof(MovementController), true);

        {
            EditorGUILayout.PrefixLabel("Input targetting mode");
            InputTargettingMode tmp = (InputTargettingMode) EditorGUILayout.EnumPopup(inputTargettingMode);
            if (inputTargettingMode != tmp) { inputTargettingMode = tmp; markRepaint = true; }
        }
        
        if(inputTargettingMode != InputTargettingMode.Direct)
        {
            EditorGUILayout.PrefixLabel("Snap threshold");
            snapInputThreshold = EditorGUILayout.Slider(snapInputThreshold, 0.01f, 1);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Time", EditorStyles.boldLabel);
        {
            EditorGUILayout.PrefixLabel("Simulation resolution");
            float tmp = EditorGUILayout.Slider(timeResolution, 0.005f, 0.03f);
            if(timeResolution != tmp) { timeResolution = tmp; markRepaint = true; }
        }
        {
            EditorGUILayout.PrefixLabel("Label resolution");
            float tmp = EditorGUILayout.Slider(timeLabelResolution, 0.1f, 1f);
            if(timeLabelResolution != tmp) { timeLabelResolution = tmp; markRepaint = true; }
        }
        {
            EditorGUILayout.PrefixLabel("Max cutoff");
            float tmp = EditorGUILayout.Slider(maxEndTime, 5f, 45f);
            if (maxEndTime != tmp) { maxEndTime = tmp; markRepaint = true; }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Physics", EditorStyles.boldLabel);
        {
            EditorGUILayout.PrefixLabel("Raycast epsilon");
            float tmp = EditorGUILayout.Slider(physicsEpsilon, 0.001f, 0.005f);
            if(physicsEpsilon != tmp) { physicsEpsilon = tmp; markRepaint = true; }
        }

        {
            EditorGUILayout.PrefixLabel("Surface normal microframes");
            int tmp = EditorGUILayout.IntSlider(nMicroframes, 1, 12);
            if(nMicroframes != tmp) { nMicroframes = tmp; markRepaint = true; }
        }

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
            List<SimulatedPathData> path = SimulatePath();
            {
                List<Vector3> vec3Path = path.ConvertAll(x => (Vector3)x.pos);
                Handles.DrawAAPolyLine(3, vec3Path.ToArray());
            }
            for(float i = 0; i < path.Count; i += timeLabelResolution/timeResolution) Handles.Label(path[(int)i].pos, path[(int)i].time.ToString("n2")+"s");
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

    private List<SimulatedPathData> SimulatePath()
    {
        List<SimulatedPathData> output = new List<SimulatedPathData>();

        SimulatedPathData data = new SimulatedPathData();
        data.pos = startPosition;
        data.vel = Vector2.zero;
        data.time = 0;

        InputParam input;
        float signBeforeMove;
        float lastGroundTime = -1000;
        float groundedness() { return 1 - Mathf.Clamp01((data.time - lastGroundTime) / character.ghostJumpTime); }
        CastFunc cast = GetCastFunc(character.gameObject);

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
                InputTargettingMode.EightWay => new Vector2((float)FacingExt.Detect(dp.x, snapInputThreshold), (float)FacingExt.Detect(dp.y, snapInputThreshold)),
                InputTargettingMode.OnlyX    => new Vector2((float)FacingExt.Detect(dp.x, snapInputThreshold),                                                 0),
                InputTargettingMode.OnlyY    => new Vector2(                                                0, (float)FacingExt.Detect(dp.y, snapInputThreshold)),
                _ => throw new System.NotImplementedException(),
            };

            //Tick time and velocity
            data.time += timeResolution;
            data.vel = a.DoPhysics(character, data.vel, new TimeParam { timeActive = data.time, delta = timeResolution }, input, groundedness(), IAction.PhysicsMode.SimulatePath);

            //Check to see if we would hit anything while moving
            float timeThisFrame = timeResolution;
            for(int i = 0; i < nMicroframes && timeThisFrame > timeResolution*0.02f; ++i)
            {
                RaycastHit2D groundCheck = cast(data.pos + Vector2.up*physicsEpsilon, data.vel*timeThisFrame);
                Vector2 groundTangent = new Vector2(groundCheck.normal.y, -groundCheck.normal.x);
                data.grounded = groundCheck.collider != null;

                //We didn't hit ground = move normally
                if (!data.grounded)
                {
                    data.pos += data.vel * timeThisFrame;
                    lastGroundTime = data.time;
                    timeThisFrame = 0;
                }
                //We hit ground = need to project along it
                else
                {
                    data.pos += groundCheck.fraction * timeThisFrame * data.vel + groundCheck.normal*physicsEpsilon;
                    data.vel = Vector2Ext.Proj(data.vel, groundTangent);
                    timeThisFrame *= 1 - groundCheck.fraction;
                    lastGroundTime = data.time;
                }
            }
            
            //Mark frame
            output.Add(data);
        }
        //Until we reach our destination, or run out of simulation time
        while (signBeforeMove == Mathf.Sign(endPosition.x - data.pos.x) && data.time < maxEndTime);

        a.DoCleanup(character, null, true);

        return output;
    }
}
