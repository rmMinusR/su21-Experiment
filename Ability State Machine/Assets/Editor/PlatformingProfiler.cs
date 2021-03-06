using System;
using System.Collections.Generic;
using System.Linq;
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
            PlayerHost m = Selection.activeGameObject.GetComponent<PlayerHost>();
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

    #region Simulation settings

    private PlayerHost character = null;

    private Vector2 startPosition = Vector2.left;
    private Vector2 endPosition   = Vector2.right;
    private float maxSimulationTime = 10f;
    private float maxBranchTime = 10f;

    private float snapInputThreshold = 0.05f;

    private float jumpLedgeProbing = 5f;
    private float ledgeThreshold = 1f;
    private bool jumpIfLedge = true;
    private bool jumpIfTooSteep = false;

    private float timeResolution = 0.01f;
    private float timeLabelResolution = 0.5f;
    private float physicsEpsilon = 0.00095f;
    private int nMicroframes = 1;

    private bool showDebugData = false;

    private InputTargettingMode inputTargettingMode = InputTargettingMode.EightWay;

    #endregion

    #region Support data

    private static List<SimulatedPathFrame> PopRange(List<SimulatedPathFrame> src, int index)
    {
        List<SimulatedPathFrame> dst = src.GetRange(index, src.Count - 1 - index);
        src.RemoveRange(index, src.Count - 1 - index);
        return dst;
    }

    private static void Split(NTree<List<SimulatedPathFrame>> src, int index)
    {
        //Split data
        NTree<List<SimulatedPathFrame>> afterSplit = new NTree<List<SimulatedPathFrame>>(PopRange(src.data, index));

        //Transfer children
        {
            LinkedList<NTree<List<SimulatedPathFrame>>> tmp = afterSplit.children;
            afterSplit.children = src.children;
            src.children = tmp;
        }

        //Link
        src.children.AddFirst(afterSplit);
    }

    private static void Merge(List<SimulatedPathFrame> dst, List<SimulatedPathFrame> toAppend)
    {
        dst.AddRange(toAppend);
        toAppend.Clear();
    }

    private struct SimulatedPathFrame
    {
        public Vector2 pos;
        public Vector2 vel;
        public float time;
        public bool grounded;

        public enum LedgeType
        {
            Falling = -1,
            None = 0,
            Rising = 1
        }
        public LedgeType ledge;

        public class CompareByTime : IComparer<SimulatedPathFrame>
        {
            public int Compare(SimulatedPathFrame x, SimulatedPathFrame y)
            {
                return Comparer<float>.Default.Compare(x.time, y.time);
            }
        }
    }

    #endregion

    private void OnGUI()
    {
        bool markRepaint = false;

        character = (PlayerHost) EditorGUILayout.ObjectField("Agent", character, typeof(PlayerHost), true);

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
            tmp = EditorGUILayout.Slider("Ledge drop threshold (m)", ledgeThreshold, 1, 10);
            if (ledgeThreshold != tmp) { ledgeThreshold = tmp; markRepaint = true; }
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
            float tmp = EditorGUILayout.Slider("Max. time simulated (s)", maxSimulationTime, 5f, 45f);
            if (maxSimulationTime != tmp) { maxSimulationTime = tmp; markRepaint = true; }
        }
        {
            float tmp = EditorGUILayout.Slider("Max. time in branch (s)", maxBranchTime, 5f, maxSimulationTime);
            if (maxBranchTime != tmp) { maxBranchTime = tmp; markRepaint = true; }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Physics", EditorStyles.boldLabel);
        {
            float tmp = EditorGUILayout.Slider("Raycast backpedal", physicsEpsilon, 0.0001f, 0.002f);
            if(physicsEpsilon != tmp) { physicsEpsilon = tmp; markRepaint = true; }
        }
        {
            int tmp = EditorGUILayout.IntSlider("Tangents processed/frame", nMicroframes, 1, 12);
            if(nMicroframes != tmp) { nMicroframes = tmp; markRepaint = true; }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Misc.", EditorStyles.boldLabel);
        {
            bool tmp = EditorGUILayout.Toggle("Show debug info", showDebugData);
            if(showDebugData != tmp) { showDebugData = tmp; markRepaint = true; }
        }

        if (markRepaint) SceneView.RepaintAll();
    }

    private void OnSceneGUI(SceneView sceneView)
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

        EditorGUI.EndChangeCheck();

        //If we have a simulatable environment
        if (character != null)
        {
            //Setup
            PlayerHost.Context context = new PlayerHost.Context(character);
            context.currentAction = character.GetComponent<BaseMovementAction>();
            context.time.delta = timeResolution;

            //Prep simulation
            HashSet<Tuple<NTree<List<SimulatedPathFrame>>, SimulatedPathFrame>> frontier = new HashSet<Tuple<NTree<List<SimulatedPathFrame>>, SimulatedPathFrame>>();
            frontier.Add(Tuple.Create<NTree<List<SimulatedPathFrame>>, SimulatedPathFrame>(null,
                new SimulatedPathFrame
                {
                    pos = startPosition,
                    vel = Vector2.zero,
                    time = 0
                }
            ));

            System.Diagnostics.Stopwatch simulationTimeTaken = new System.Diagnostics.Stopwatch();
            simulationTimeTaken.Start();

            NTree<List<SimulatedPathFrame>> pathsTree = SimulateTreeForward(frontier, context);

            simulationTimeTaken.Stop();
            long microseconds = simulationTimeTaken.ElapsedTicks / (System.Diagnostics.Stopwatch.Frequency / (1000L*1000L));
            if(showDebugData) Debug.Log("Finished simulating in "+(microseconds/1000.0f)+"ms");

            //Render all
            pathsTree.Traverse(segmentNode => RenderPathSegment(segmentNode.data));
            if(showDebugData) Debug.Log("========");
        }
    }

    private void RenderPathSegment(List<SimulatedPathFrame> segment)
    {
        //Render basic path
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(3, segment.ConvertAll(x => (Vector3)x.pos).ToArray());

        //Render time labels
        for (float i = 0; i < segment.Count; i += timeLabelResolution / timeResolution) Handles.Label(segment[(int)i].pos, segment[(int)i].time.ToString("n2") + "s");

        //Render head to show forking
        if (showDebugData)
        {
            Handles.color = Color.yellow;
            Handles.DrawWireCube(segment[0].pos, Vector3.one * 0.05f);
        }

        //Render ledge debug data
        if (showDebugData) foreach (SimulatedPathFrame ledge in segment.Where(x => x.ledge != SimulatedPathFrame.LedgeType.None))
        {
            if(ledge.ledge == SimulatedPathFrame.LedgeType.Rising ) Handles.color = Color.cyan;
            if(ledge.ledge == SimulatedPathFrame.LedgeType.Falling) Handles.color = Color.red;
            Handles.DrawWireCube(ledge.pos, Vector3.one * 0.15f);
        }
        
    }

    private NTree<List<SimulatedPathFrame>> SimulateTreeForward(HashSet<Tuple<NTree<List<SimulatedPathFrame>>, SimulatedPathFrame>> frontier, PlayerHost.Context context)
    {
        CastFunc playerCast = GetCastFunc(character.gameObject);
        NTree<List<SimulatedPathFrame>> pathsTree = null;

        //Simulate until all paths are resolved, or we found a way to the end
        bool foundSolution = false;
        while (frontier.Count > 0 && !foundSolution) {
            //Pop one element
            Tuple<NTree<List<SimulatedPathFrame>>, SimulatedPathFrame> next = frontier.First();
            frontier.Remove(next);
            NTree<List<SimulatedPathFrame>> parent = next.Item1;
            SimulatedPathFrame start = next.Item2;

            //Safety escape condition: If it ran out of simulation time, skip
            if(start.time > maxSimulationTime) continue;

            //Ensure Context isn't stale
            context.time.active = context.time.stable = start.time;

            //Simulate forward
            List<SimulatedPathFrame> forwardPath = new List<SimulatedPathFrame> { start };
            SimulateSegmentForward(context, ref forwardPath,
                (vPrev, vCur) => vPrev.grounded != vCur.grounded //Has ground state changed?
                                || (foundSolution |= Mathf.Sign(endPosition.x-vPrev.pos.x) != Mathf.Sign(endPosition.x-vCur.pos.x)), //Have we reached our target?
                (v) => false
            ); //Context won't bleed over, but lastGroundTime won't either. FIXME

            //Add to path tree
            NTree<List<SimulatedPathFrame>> forwardPathTreeNode = new NTree<List<SimulatedPathFrame>>(forwardPath);
            if (parent != null) parent.children.AddLast(forwardPathTreeNode);
            else if(pathsTree == null) pathsTree = forwardPathTreeNode;

            //Push to frontier
            SimulatedPathFrame stub = forwardPath[forwardPath.Count - 1];
            frontier.Add(Tuple.Create(forwardPathTreeNode, stub));
            forwardPath.RemoveAt(forwardPath.Count - 1); //Pop last frame since it will be of different state

            if(showDebugData) Debug.Log(forwardPath[0].pos + " => " + forwardPath[forwardPath.Count - 1].pos);

            if (parent != null)
            {
                //Backtrack and validate for each ledge
                foreach(SimulatedPathFrame ledge in forwardPath.Where(x => x.ledge == SimulatedPathFrame.LedgeType.Rising))
                {
                    //Raycast to find where we expect to land
                    Vector2 expectedLandingPosition = playerCast(ledge.pos, Physics2D.gravity.normalized*jumpLedgeProbing).point;
                    Vector2 expectedStartingPosition = parent.data[parent.data.Count-1].pos + (expectedLandingPosition - stub.pos);
                    if(showDebugData) Debug.Log("Ledge at "+expectedLandingPosition);

                    //Start with pivot as closest point to our predicted start point
                    int pivot = 0;
                    float closestApproach = float.MaxValue;
                    for(int i = 0; i < parent.data.Count; ++i) {
                        float dist = Vector2.Distance(parent.data[i].pos, expectedStartingPosition);
                        if (dist < closestApproach)
                        {
                            closestApproach = dist;
                            pivot = i;
                        }
                    }

                    List<SimulatedPathFrame> backtrackedPath = BinarySearchForAncestor(context, parent.data, ref pivot, expectedLandingPosition);
                    
                    //TODO repeat with parent's parent if pivot=0
                        
                    //Merge head into parent
                    //Split(parent, pivot); //FIXME breaks everything by allowing empty list
                    parent.children.AddLast(new NTree<List<SimulatedPathFrame>>(backtrackedPath)); //Parent is now the first part of forwardPath's parent

                    //TODO merge tail/prune
                }

            }

            //TODO path merging/pruning here as well?
        }

        return pathsTree;
    }

    private List<SimulatedPathFrame> BinarySearchForAncestor(PlayerHost.Context context, List<SimulatedPathFrame> frames, ref int pivot, Vector2 targetPos)
    {
        List<SimulatedPathFrame> backtrackedPath = new List<SimulatedPathFrame>();
        //Binary search until we've found the specific frames
        int lowerBound = 0;
        int upperBound = frames.Count-1;
        int iterationCounter = 0;
        do
        {
            iterationCounter++;

            //Reset for next run through
            backtrackedPath.Clear();
            backtrackedPath.Add(frames[pivot]);

            //Run simulation forward to check if our prediction is valid
            SimulateSegmentForward(context, ref backtrackedPath,
                (vPrev, vCur) => (vCur.grounded && !vPrev.grounded) //Did we just hit ground?
                                || (vCur.time-backtrackedPath[0].time) > maxBranchTime //Did we hit the simulation threshold?
                                || (vCur.pos.y < targetPos.y && Vector2.Dot(Physics2D.gravity, vCur.vel) > 0), //Are we moving away from our target location? (Only after we've started falling again)
                (v) => true
            );

            //FIXME bad practice, find a better comparison
            Vector2 targetOffset = backtrackedPath[backtrackedPath.Count-1].pos - targetPos;
            if (targetOffset.x < 0) lowerBound = pivot; //delta-X is negative, we undershot
            else                    upperBound = pivot; //delta-X is positive, we overshot

            if(showDebugData) Debug.Log(iterationCounter+": range "+lowerBound+" to "+upperBound+" = "+frames[lowerBound].pos+" to "+frames[upperBound].pos+" // status = "+targetOffset.x);

            //If we're continuing, update pivot (rounds down)
            if (upperBound - lowerBound > 1) pivot = (lowerBound+upperBound) / 2;

        } while (upperBound-lowerBound > 1); //1 frame accuracy
        if(showDebugData) Debug.Log("Found pivot="+pivot+" after "+iterationCounter+" iterations");

        backtrackedPath.TrimExcess();
        return backtrackedPath;
    }

    private void SimulateSegmentForward(PlayerHost.Context context, ref List<SimulatedPathFrame> path, Func<SimulatedPathFrame, SimulatedPathFrame, bool> shouldStop, Func<SimulatedPathFrame, bool> extraJumpConditions)
    {
        SimulatedPathFrame data = path[path.Count-1];

        CastFunc cast = GetCastFunc(character.gameObject);
        Vector2 colliderSize = GetColliderSize(character.gameObject);

        context.currentAction.DoSetup(ref context, null, IAction.ExecMode.SimulatePath);
        
        do
        {
            //Run ledge detection
            RaycastHit2D ledgeDet0, ledgeDet1;
            data.ledge = DetectLedge(context, data, colliderSize, out ledgeDet0, out ledgeDet1);

            //Setup input
            context.input.local = context.input.global = SimulateAxisInput(data.pos);
            context.input.jump = SimulateJumpInput(context, data, ledgeDet0, ledgeDet1) || extraJumpConditions(data);

            //Tick time and simulate integration
            data.time = context.time.active = context.time.stable += context.time.delta;
            data.vel = character.DoPhysicsUpdate(data.vel, ref context, IAction.ExecMode.SimulatePath);

            //Simulate collision response
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
            path.Add(data);
        }
        //Run until we run out of simulation time, or we're told to stop
        while (data.time < maxSimulationTime && !shouldStop(path[path.Count - 2], path[path.Count - 1]));

        context.currentAction.DoCleanup(ref context, null, IAction.ExecMode.SimulatePath);
    }
    
    private SimulatedPathFrame.LedgeType DetectLedge(PlayerHost.Context context, SimulatedPathFrame data, Vector2 colliderSize, out RaycastHit2D ledgeDet0, out RaycastHit2D ledgeDet1)
    {
        Vector2 posPlusVel = data.pos + data.vel * context.time.delta; //Next frame
        ledgeDet0 = Physics2D.Raycast(data.pos, Physics2D.gravity.normalized, jumpLedgeProbing+colliderSize.y); //Where we're standing right now
        float here = ledgeDet0.collider != null ? ledgeDet0.distance : float.MaxValue;
        ledgeDet1 = Physics2D.Raycast(posPlusVel, Physics2D.gravity.normalized, jumpLedgeProbing+colliderSize.y); //Where we'll be standing once update is done
        float there = ledgeDet1.collider != null ? ledgeDet1.distance : float.MaxValue;
        
        //Run ledge detection
        float dy = here - there; //Inverted because these are technically distance down, not distance up
        if(Mathf.Abs(dy) > ledgeThreshold) return (dy>0) ? SimulatedPathFrame.LedgeType.Rising : SimulatedPathFrame.LedgeType.Falling;
        else return SimulatedPathFrame.LedgeType.None;
    }

    private bool SimulateJumpInput(PlayerHost.Context context, SimulatedPathFrame data, RaycastHit2D ledgeDet0, RaycastHit2D ledgeDet1)
    {
        //Jump-if-ledge
        if(jumpIfLedge && data.ledge != SimulatedPathFrame.LedgeType.None) return true;

        //Run steep slope detection
        if (jumpIfTooSteep && ledgeDet1.collider != null //Do we have a slope that can even be counted?
            && Mathf.Sign(ledgeDet1.normal.x) != Mathf.Sign(data.vel.x) //Are we moving against the slope?
            && Mathf.Abs(Vector2.Angle(-ledgeDet1.normal, Physics2D.gravity)) > context.owner.maxGroundAngle //Compare angles
        ) return true;

        //Fallback
        return false;
    }

    private Vector2 SimulateAxisInput(Vector2 curPos)
    {
        //Calculate input
        Vector2 dp = endPosition - curPos;
        return inputTargettingMode switch
        {
            InputTargettingMode.Direct   => dp.normalized,
            InputTargettingMode.EightWay => new Vector2((float)FacingExt.Detect(dp.x, snapInputThreshold), (float)FacingExt.Detect(dp.y, snapInputThreshold)),
            InputTargettingMode.OnlyX    => new Vector2((float)FacingExt.Detect(dp.x, snapInputThreshold),                                                 0),
            InputTargettingMode.OnlyY    => new Vector2(                                                0, (float)FacingExt.Detect(dp.y, snapInputThreshold)),
            _ => throw new System.NotImplementedException(),
        };
    }

}
