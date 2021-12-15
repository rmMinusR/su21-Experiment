using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Pathfinding
{
    public sealed class Pathfinder : EditorWindow
    {
        #region Support code

        public enum InputTargettingMode
        {
            Direct,
            EightWay,
            OnlyX,
            OnlyY
        }

        [MenuItem("Tools/Platforming Pathfinder")]
        public static void Open()
        {
            EditorWindow.GetWindow(typeof(Pathfinder));
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

        private SurfaceSweeper surfaceSweeper;

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

        private void OnGUI()
        {
            bool markRepaint = false;

            character = (PlayerHost) EditorGUILayout.ObjectField("Agent", character, typeof(PlayerHost), true);

            EditorGUILayout.Space();
            if(GUILayout.Button("Scan surfaces"))
            {
                surfaceSweeper = new SurfaceSweeper();
                surfaceSweeper.ScanFrom(startPosition);
                markRepaint = true;
            }

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


            if(surfaceSweeper != null) surfaceSweeper.DebugDraw();
        }
    }
}