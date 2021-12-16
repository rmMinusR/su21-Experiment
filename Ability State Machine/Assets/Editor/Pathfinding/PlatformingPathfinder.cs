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

            //Set recommended values
            detectionResolution = 2;
            surfaceResolution = 0.3f;
            mergeDist = 0.5f;
            sweepBackpedal = 0.5f;
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

        private WorldRepresentation worldRepr;

        private float detectionResolution;
        private float surfaceResolution;
        private float mergeDist;
        private float sweepBackpedal;

        #endregion

        private void OnGUI()
        {
            bool markRepaint = false;

            character = (PlayerHost) EditorGUILayout.ObjectField("Agent", character, typeof(PlayerHost), true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Surface scanning", EditorStyles.boldLabel);
            detectionResolution = EditorGUILayout.Slider("Detection step"          , detectionResolution, 0.05f, 5f);
            surfaceResolution   = EditorGUILayout.Slider("Surface resolution"      , surfaceResolution  , 0.05f, detectionResolution);
            mergeDist           = EditorGUILayout.Slider("Surface connection dist.", mergeDist          , surfaceResolution, detectionResolution);
            sweepBackpedal      = EditorGUILayout.Slider("Surface backpedal"       , sweepBackpedal     , 0.05f, 1f);
            GUI.enabled = (character != null);
            if (GUILayout.Button("Scan walkable surfaces"))
            {
                worldRepr = new WorldRepresentation();
                worldRepr.ScanAll(detectionResolution, mergeDist, surfaceResolution, sweepBackpedal, character.maxGroundAngle, x => x == character.gameObject);
                markRepaint = true;
            }
            GUI.enabled = true;
            if (worldRepr != null && GUILayout.Button("Clear walkable surfaces"))
            {
                worldRepr = null;
                markRepaint = true;
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


            if(worldRepr != null) worldRepr.DebugDraw();
        }
    }
}