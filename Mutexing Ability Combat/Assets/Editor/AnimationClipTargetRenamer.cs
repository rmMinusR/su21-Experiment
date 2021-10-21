using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Animation clip target renamer.
/// This script allows animation curves to be moved from one target to another.
/// 
/// Usage:
///     1) Open the Animation Clip Target Renamer from the Window menu in the Unity UI.
///     2) Select the animation clip whose curves you wish to move.
///     3) Change the names in the textboxes on the right side of the window to the names of the objects you wish to move the animations to.
///     4) Press Apply.
/// </summary>
public class AnimationClipTargetRenamer : EditorWindow
{

    public AnimationClip selectedClip;


    /// <summary>
    /// The curve data for the animation.
    /// </summary>
    private EditorCurveBinding[] curveBindings;
    private AnimationCurve[] curveDatas;

    /// <summary>
    /// The names of the original GameObjects.
    /// </summary>
    private List<string> origObjectPaths;


    /// <summary>
    /// The names of the target GameObjects.
    /// </summary>
    private List<string> targetObjectPaths;

    private bool initialized;

    [MenuItem("Tools/Animation Clip Target Renamer")]
    public static void OpenWindow()
    {
        AnimationClipTargetRenamer renamer = GetWindow<AnimationClipTargetRenamer> ("Animation Clip Target Renamer");
        renamer.Clear();
    }

    private void Initialize()
    {
        curveBindings = AnimationUtility.GetCurveBindings(selectedClip);
        curveDatas = curveBindings.Select(c => AnimationUtility.GetEditorCurve(selectedClip, c)).ToArray();

        origObjectPaths = new List<string>();
        targetObjectPaths = new List<string>();
        foreach (EditorCurveBinding curveData in curveBindings)
        {
            if (curveData.path != "" && !origObjectPaths.Contains(curveData.path))
            {
                origObjectPaths.Add(curveData.path);
                targetObjectPaths.Add(curveData.path);
            }
        }
        initialized = true;
    }

    private void Clear()
    {
        curveBindings = null;
        origObjectPaths = null;
        targetObjectPaths = null;
        initialized = false;
    }

    private void RenameTargets()
    {
        // set the curve data to the new values. 
        for (int i = 0; i < targetObjectPaths.Count; i++)
        {
            string oldName = origObjectPaths[i];
            string newName = targetObjectPaths[i];

            if (oldName != newName)
            {
                for(int j = 0; j < curveBindings.Length; ++j)
                {
                    if (curveBindings[j].path == oldName)
                    {
                        curveBindings[j].path = newName;
                    }
                }
            }
        }

        // set up the curves based on the new names.
        selectedClip.ClearCurves();
        for (int j = 0; j < curveBindings.Length; ++j)
        {
            selectedClip.SetCurve(curveBindings[j].path, curveBindings[j].type, curveBindings[j].propertyName, curveDatas[j]);
        }
        Clear();
        Initialize();
    }

    void OnGUIShowTargetsList()
    {
        // if we got here, we have all the data we need to work with,
        // so we should be able to build the UI.

        // build the list of textboxes for renaming.
        if (targetObjectPaths != null)
        {
            EditorGUILayout.Space();
            EditorGUIUtility.labelWidth = 250;

            for (int i = 0; i < targetObjectPaths.Count; i++)
            {
                string newName = EditorGUILayout.TextField(origObjectPaths[i], targetObjectPaths[i]);

                if (targetObjectPaths[i] != newName)
                {
                    targetObjectPaths[i] = newName;
                }
            }
        }
    }

    void OnGUI()
    {
        AnimationClip previous = selectedClip;
        selectedClip = EditorGUILayout.ObjectField("Animation Clip", selectedClip, typeof(AnimationClip), true) as AnimationClip;

        if (selectedClip != previous)
        {
            Clear();
        }

        if (selectedClip != null)
        {
            if (!initialized)
            {
                Initialize();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh"))
            {
                Clear();
                Initialize();
            }
            EditorGUILayout.EndHorizontal();

            OnGUIShowTargetsList();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Apply"))
            {
                RenameTargets();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

}