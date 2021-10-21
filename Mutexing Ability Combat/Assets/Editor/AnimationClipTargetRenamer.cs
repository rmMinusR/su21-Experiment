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
    
    private RenameUtils.RenameInfo[] renameInfos;

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
            AnimationUtility.GetCurveBindings               (selectedClip).Select(b => new RenameUtils.RenameInfo(b)),
            AnimationUtility.GetObjectReferenceCurveBindings(selectedClip).Select(b => new RenameUtils.RenameInfo(b))
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
                renameInfos[i].targetPath = newName;
            }
        }

        // set up the curves based on the new names.
        RenameUtils.WriteInfo[] writers = Enumerable.Union(
            AnimationUtility.GetCurveBindings               (selectedClip).Select(b => (RenameUtils.WriteInfo) new RenameUtils.WriteInfoFloat (b, selectedClip)),
            AnimationUtility.GetObjectReferenceCurveBindings(selectedClip).Select(b => (RenameUtils.WriteInfo) new RenameUtils.WriteInfoObject(b, selectedClip))
        ).ToArray();

        selectedClip.ClearCurves();
        for (int i = 0; i < renameInfos.Length; ++i) writers[i].WriteTo(selectedClip, renameInfos[i]);

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


namespace RenameUtils
{
    internal sealed class RenameInfo
    {
        public readonly string originalPath;
        public readonly string originalProperty;
        public string targetPath;
        public string targetProperty;

        public RenameInfo(EditorCurveBinding binding)
        {
            originalPath     = targetPath     = binding.path;
            originalProperty = targetProperty = binding.propertyName;
        }

        public void Update(ref EditorCurveBinding binding)
        {
            if(originalPath == binding.path && originalProperty == binding.propertyName)
            {
                binding.path         = targetPath;
                binding.propertyName = targetProperty;
            }
        }
    }

    internal abstract class WriteInfo
    {
        public EditorCurveBinding binding;

        public WriteInfo(EditorCurveBinding binding)
        {
            this.binding = binding;
        }

        public abstract void WriteTo(AnimationClip clip, RenameInfo rename);
    }

    internal sealed class WriteInfoFloat : WriteInfo
    {
        public readonly AnimationCurve data;

        public WriteInfoFloat(EditorCurveBinding binding, AnimationClip clip) : base(binding)
        {
            data = AnimationUtility.GetEditorCurve(clip, binding);
        }

        public override void WriteTo(AnimationClip clip, RenameInfo rename)
        {
            rename.Update(ref binding);
            AnimationUtility.SetEditorCurve(clip, binding, data);
        }
    }

    internal sealed class WriteInfoObject : WriteInfo
    {
        public readonly ObjectReferenceKeyframe[] data;

        public WriteInfoObject(EditorCurveBinding binding, AnimationClip clip) : base(binding)
        {
            data = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        }

        public override void WriteTo(AnimationClip clip, RenameInfo rename)
        {
            rename.Update(ref binding);
            AnimationUtility.SetObjectReferenceCurve(clip, binding, data);
        }
    }
}