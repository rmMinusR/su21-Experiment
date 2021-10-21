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
public class AnimationClipTargetBatchRenamer : EditorWindow
{

    private List<RenameUtils.RenameInfo> __renameInfos;
    private List<RenameUtils.RenameInfo> renameInfos => __renameInfos ??= new List<RenameUtils.RenameInfo>();

    private List<AnimationClip> __selectedClips;
    private List<AnimationClip> selectedClips => __selectedClips ??= new List<AnimationClip>();


    private IEnumerable<AnimationClip> currentSelection
    {
        get
        {
            return Selection.assetGUIDs.Select(id => AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(id)) as AnimationClip).Where(asset => asset != null);
        }
    }

    private bool initialized;

    [MenuItem("Tools/Animation Clip Target Batch Renamer")]
    public static void OpenWindow()
    {
        AnimationClipTargetBatchRenamer renamer = GetWindow<AnimationClipTargetBatchRenamer> ("Animation Clip Target Batch Renamer");
        renamer.Clear();
    }

    private void Initialize()
    {
        selectedClips.Clear();
        selectedClips.AddRange(currentSelection);

        renameInfos.Clear();
        foreach(AnimationClip clip in selectedClips)
        {
            foreach (EditorCurveBinding binding in Enumerable.Union(
                AnimationUtility.GetCurveBindings(clip),
                AnimationUtility.GetObjectReferenceCurveBindings(clip)
            ))
            {
                //Prevent duplicates
                if (!renameInfos.Any(x => x.originalPath == binding.path && x.originalProperty == binding.propertyName)) renameInfos.Add(new RenameUtils.RenameInfo(binding));
            }
        }
        
        initialized = true;
    }

    private void Clear()
    {
        initialized = false;
    }

    private void RenameTargets()
    {
        foreach(AnimationClip clip in selectedClips)
        {
            // set up the curves based on the new names.
            RenameUtils.WriteInfo[] writers = Enumerable.Union(
                AnimationUtility.GetCurveBindings               (clip).Select(b => (RenameUtils.WriteInfo) new RenameUtils.WriteInfoFloat (b, clip)),
                AnimationUtility.GetObjectReferenceCurveBindings(clip).Select(b => (RenameUtils.WriteInfo) new RenameUtils.WriteInfoObject(b, clip))
            ).ToArray();

            clip.ClearCurves();
            foreach (RenameUtils.WriteInfo writer in writers)
            {
                RenameUtils.RenameInfo rename = renameInfos.FirstOrDefault(r => r.originalPath == writer.binding.path && r.originalProperty == writer.binding.propertyName);
                if (rename != null) writer.WriteTo(clip, rename);
            }
        }

        //Refresh
        Clear();
        Initialize();
    }

    void OnGUIShowTargetsList()
    {
        //EditorGUIUtility.labelWidth = 250;

        for (int i = 0; i < renameInfos.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(renameInfos[i].originalPath + ":" + renameInfos[i].originalProperty);
            renameInfos[i].targetPath     = EditorGUILayout.TextField(renameInfos[i].targetPath    );
            renameInfos[i].targetProperty = EditorGUILayout.TextField(renameInfos[i].targetProperty);
            EditorGUILayout.EndHorizontal();
        }
    }

    void OnGUI()
    {
        if (GUILayout.Button("Refresh")) Clear();

        if (!initialized) Initialize();

        EditorGUILayout.Space();
        OnGUIShowTargetsList();

        EditorGUILayout.Space();
        if (GUILayout.Button("Apply")) RenameTargets();
    }

}