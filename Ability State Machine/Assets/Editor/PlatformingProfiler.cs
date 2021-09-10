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
            if (coll is CapsuleCollider2D cc) return (pos, dir) => Physics2D.CapsuleCast(pos+cc.offset, cc.size*cc.transform.localScale, cc.direction, 0, dir.normalized, dir.magnitude);
            else Debug.LogWarning("Unsupported collider: " + coll.GetType().Name);
        }

        throw new System.NotImplementedException();
    }
    
    private static Vector2 GetColliderSize(GameObject obj)
    {
        foreach (Collider2D coll in obj.GetComponents<Collider2D>())
        {
            if (coll is CapsuleCollider2D cc) return cc.size*cc.transform.localScale;
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

    private float jumpLedgeProbing = 5f;
    private float jumpLedgeThreshold = 1f;
    private bool jumpIfLedge = true;
    private bool jumpIfTooSteep = false;

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

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Input settings", EditorStyles.boldLabel);
        {
            InputTargettingMode tmp = (InputTargettingMode) EditorGUILayout.EnumPopup("Input targetting mode", inputTargettingMode);
            if (inputTargettingMode != tmp) { inputTargettingMode = tmp; markRepaint = true; }
        }
        if (inputTargettingMode != InputTargettingMode.Direct) {
            float tmp = EditorGUILayout.Slider("Snap threshold", snapInputThreshold, 0.01f, 1);
            if (snapInputThreshold != tmp) { snapInputThreshold = tmp; markRepaint = true; }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Jump conditions", EditorStyles.boldLabel);
        {
            bool tmp = EditorGUILayout.Toggle("Slope too steep", jumpIfTooSteep);
            if (jumpIfTooSteep != tmp) { jumpIfTooSteep = tmp; markRepaint = true; }
        }
        {
            bool tmp = EditorGUILayout.Toggle("Approaching ledge", jumpIfLedge);
            if (jumpIfLedge != tmp) { jumpIfLedge = tmp; markRepaint = true; }
        }
        if(jumpIfLedge) {
            float tmp = EditorGUILayout.Slider("Ledge probe (m)", jumpLedgeProbing, 5, 50);
            if (jumpLedgeProbing != tmp) { jumpLedgeProbing = tmp; markRepaint = true; }
            tmp = EditorGUILayout.Slider("Ledge drop threshold (m)", jumpLedgeThreshold, 1, 10);
            if (jumpLedgeThreshold != tmp) { jumpLedgeThreshold = tmp; markRepaint = true; }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Time", EditorStyles.boldLabel);
        {
            float tmp = 1/EditorGUILayout.Slider("Simulation resolution (FPS)", Mathf.Clamp(1/timeResolution, 60, 240), 60, 240);
            if(timeResolution != tmp) { timeResolution = tmp; markRepaint = true; }
        }
        {
            float tmp = EditorGUILayout.Slider("Label interval (s)", timeLabelResolution, 0.1f, 1f);
            if(timeLabelResolution != tmp) { timeLabelResolution = tmp; markRepaint = true; }
        }
        {
            float tmp = EditorGUILayout.Slider("Max. duration simulated (s)", maxEndTime, 5f, 45f);
            if (maxEndTime != tmp) { maxEndTime = tmp; markRepaint = true; }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Physics", EditorStyles.boldLabel);
        {
            float tmp = EditorGUILayout.Slider("Raycast backpedal", physicsEpsilon, 0.001f, 0.005f);
            if(physicsEpsilon != tmp) { physicsEpsilon = tmp; markRepaint = true; }
        }
        {
            int tmp = EditorGUILayout.IntSlider("Tangents processed/frame", nMicroframes, 1, 12);
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
        if (character == null)
        {
            Handles.color = Color.green;
            EditorExt.DrawArrow(startPosition, endPosition, Vector3.forward, (p1, p2) => Handles.DrawAAPolyLine(p1, p2));
        }

        //Draw raycast down from start and end pos
        Handles.color = Color.red;
        {
            RaycastHit2D rc = Physics2D.Raycast(startPosition, Vector2.down, 100f);
            if (rc.collider != null) Handles.DrawDottedLine(startPosition, rc.collider != null ? rc.point : (endPosition + Vector2.down * 100f), 1);
        }
        {
            RaycastHit2D rc = Physics2D.Raycast(endPosition, Vector2.down, 100f);
            Handles.DrawDottedLine(endPosition, rc.collider != null ? rc.point : (endPosition + Vector2.down * 100f), 1);
        }

        if (character != null)
        {
            //Setup
            MovementController.Context c = new MovementController.Context(character);
            c.currentAction = character.GetComponent<BaseMovementAction>();
            c.time.delta = timeResolution;

            //Simulate
            List<SimulatedPathData> path = SimulatePath(c);

            //Render basic path
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(3, path.ConvertAll(x => (Vector3)x.pos).ToArray());

            //Render time labels
            for(float i = 0; i < path.Count; i += timeLabelResolution/timeResolution) Handles.Label(path[(int)i].pos, path[(int)i].time.ToString("n2")+"s");

            //Render jump timing labels
            //TODO move to own method
            RaycastHit2D? lastFrame = Physics2D.Raycast(path[0].pos, Physics2D.gravity.normalized, jumpLedgeProbing);
            for (int i = 1; i < path.Count; ++i)
            {
                if(!path[i].grounded)// && Vector2.Dot(path[i].vel, Physics2D.gravity) > 0)
                {
                    RaycastHit2D thisFrame = Physics2D.Raycast(path[i].pos, Physics2D.gravity.normalized, jumpLedgeProbing);

                    if (lastFrame != null && thisFrame.collider != null //Detect when a collider first enters our raycast
                    && (lastFrame.Value.collider == null || Mathf.Abs(thisFrame.distance-lastFrame.Value.distance) > jumpLedgeThreshold)) //And when it's counted as a ledge
                    {
                        //Calculate time to impact
                        //Uses Y only but X would prob work too
                        float s = -thisFrame.distance;
                        float v = path[i].vel.y;
                        float a = Physics2D.gravity.y;

                        //s = ut + 1/2 * at^2
                        //t = (-u + sqrt(2sa+u^2))/a
                        float t = -(v + Mathf.Sqrt(2*s*a+v*v))/a;

                        Handles.color = Color.cyan;
                        Handles.DrawDottedLine(path[i].pos, thisFrame.point, 2);
                        Handles.Label(thisFrame.point, (t*1000).ToString("n0")+"ms to react");
                    }
                    lastFrame = thisFrame;
                } else lastFrame = null;
            }
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

    private List<SimulatedPathData> SimulatePath(MovementController.Context context)
    {
        List<SimulatedPathData> output = new List<SimulatedPathData>();

        SimulatedPathData data = new SimulatedPathData();
        data.pos = startPosition;
        data.vel = Vector2.zero;
        data.time = 0;

        float signBeforeMove;
        CastFunc cast = GetCastFunc(character.gameObject);
        Vector2 colliderSize = GetColliderSize(character.gameObject);

        context.currentAction.DoSetup(ref context, null, IAction.PhysicsMode.SimulatePath);
        
        output.Add(data);

        do
        {
            //Update escape condition
            signBeforeMove = Mathf.Sign(endPosition.x - data.pos.x);

            //Calculate input
            Vector2 dp = endPosition - data.pos;
            context.input.local = context.input.global = inputTargettingMode switch
            {
                InputTargettingMode.Direct   => dp.normalized,
                InputTargettingMode.EightWay => new Vector2((float)FacingExt.Detect(dp.x, snapInputThreshold), (float)FacingExt.Detect(dp.y, snapInputThreshold)),
                InputTargettingMode.OnlyX    => new Vector2((float)FacingExt.Detect(dp.x, snapInputThreshold),                                                 0),
                InputTargettingMode.OnlyY    => new Vector2(                                                0, (float)FacingExt.Detect(dp.y, snapInputThreshold)),
                _ => throw new System.NotImplementedException(),
            };

            //Decide whether to jump
            {
                Vector2 posPlusVel = data.pos + data.vel * context.time.delta * 4;
                RaycastHit2D ledgeDet0 = Physics2D.Raycast(data.pos, Physics2D.gravity.normalized, jumpLedgeProbing+colliderSize.y); //Where we're standing right now
                Vector2 here = ledgeDet0.collider != null ? ledgeDet0.point : (data.pos + Vector2.down * (jumpLedgeProbing+colliderSize.y));
                RaycastHit2D ledgeDet1 = Physics2D.Raycast(posPlusVel, Physics2D.gravity.normalized, jumpLedgeProbing+colliderSize.y); //Where we'll be standing once update is done
                Vector2 there = ledgeDet1.collider != null ? ledgeDet1.point : (posPlusVel + Vector2.down * (jumpLedgeProbing+colliderSize.y));

                context.input.jump = false;

                float dy = there.y - here.y;
                
                //Run ledge detection if our target is higher than we are right now
                if(jumpIfLedge && endPosition.y > data.pos.y) context.input.jump |= dy < -jumpLedgeThreshold;

                //Run steep slope detection
                if (jumpIfTooSteep && ledgeDet1.collider != null //Do we have a slope that can even be counted?
                    && Mathf.Sign(ledgeDet1.normal.x) != Mathf.Sign(data.vel.x) //Are we moving against the slope?
                    && Mathf.Abs(Vector2.Angle(-ledgeDet1.normal, Physics2D.gravity)) > context.owner.maxGroundAngle //Compare angles
                ) context.input.jump = true;
            }

            //Tick time and calculate velocity
            data.time = context.time.active = context.time.stable += context.time.delta;
            data.vel = character.DoPhysicsUpdate(data.vel, ref context, IAction.PhysicsMode.SimulatePath);

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
                    timeThisFrame = 0;
                }
                //We hit ground = need to project along it
                else
                {
                    data.pos += groundCheck.fraction * timeThisFrame * data.vel + groundCheck.normal*physicsEpsilon;
                    data.vel = Vector2Ext.Proj(data.vel, groundTangent);
                    timeThisFrame *= 1 - groundCheck.fraction;
                    context.MarkGrounded();
                }
            }
            
            //Mark frame
            output.Add(data);
        }
        //Until we reach our destination, or run out of simulation time
        while (signBeforeMove == Mathf.Sign(endPosition.x - data.pos.x) && data.time < maxEndTime);

        context.currentAction.DoCleanup(ref context, null, IAction.PhysicsMode.SimulatePath);

        return output;
    }
}
