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

    private abstract class RenameInfo
    {
        public EditorCurveBinding binding;

        public readonly string originalPath;
        public string targetPath;

        public RenameInfo(EditorCurveBinding binding)
        {
            this.binding = binding;
            originalPath = targetPath = binding.path+":"+binding.propertyName;
        }

        public abstract void WriteTo(AnimationClip clip);
    }

    private sealed class RenameInfoFloat : RenameInfo
    {
        public readonly AnimationCurve data;

        public RenameInfoFloat(EditorCurveBinding binding, AnimationClip clip) : base(binding)
        {
            data = AnimationUtility.GetEditorCurve(clip, binding);
        }

        public override void WriteTo(AnimationClip clip)
        {
            binding.path         = targetPath.Split(':')[0];
            binding.propertyName = targetPath.Split(':')[1];
            AnimationUtility.SetEditorCurve(clip, binding, data);
        }
    }

    private sealed class RenameInfoObject : RenameInfo
    {
        public readonly ObjectReferenceKeyframe[] data;

        public RenameInfoObject(EditorCurveBinding binding, AnimationClip clip) : base(binding)
        {
            data = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        }

        public override void WriteTo(AnimationClip clip)
        {
            binding.path         = targetPath.Split(':')[0];
            binding.propertyName = targetPath.Split(':')[1];
            AnimationUtility.SetObjectReferenceCurve(clip, binding, data);
        }

    }

    private RenameInfo[] renameInfos;

    private bool initialized;

    [MenuItem("Tools/Animation Clip Target Renamer")]
    public static void OpenWindow()
    {
        AnimationClipTargetRenamer renamer = GetWindow<AnimationClipTargetRenamer> ("Animation Clip Target Renamer");
        renamer.Clear();
    }

    private void Initialize()
    {
        renameInfos = Enumerable.Union(
            AnimationUtility.GetCurveBindings               (selectedClip).Select(b => (RenameInfo) new RenameInfoFloat (b, selectedClip)),
            AnimationUtility.GetObjectReferenceCurveBindings(selectedClip).Select(b => (RenameInfo) new RenameInfoObject(b, selectedClip))
        ).ToArray();
        
        initialized = true;
    }

    private void Clear()
    {
        renameInfos = null;
        initialized = false;
    }

    private void RenameTargets()
    {
        // set the curve data to the new values. 
        for (int i = 0; i < renameInfos.Length; i++)
        {
            string oldName = renameInfos[i].originalPath;
            string newName = renameInfos[i].targetPath;

            if (oldName != newName)
            {
                renameInfos[i].binding.path = newName;
            }
        }

        // set up the curves based on the new names.
        selectedClip.ClearCurves();
        for (int i = 0; i < renameInfos.Length; ++i)
        {
            renameInfos[i].WriteTo(selectedClip);
        }

        //Refresh
        Clear();
        Initialize();
    }

    void OnGUIShowTargetsList()
    {
        // if we got here, we have all the data we need to work with,
        // so we should be able to build the UI.

        // build the list of textboxes for renaming.
        if (renameInfos != null)
        {
            EditorGUILayout.Space();
            EditorGUIUtility.labelWidth = 250;

            for (int i = 0; i < renameInfos.Length; i++)
            {
                renameInfos[i].targetPath = EditorGUILayout.TextField(renameInfos[i].originalPath, renameInfos[i].targetPath);
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