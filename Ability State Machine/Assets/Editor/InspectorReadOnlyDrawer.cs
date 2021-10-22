using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InspectorReadOnlyAttribute))]
public class InspectorReadOnlyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property,
                                            GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    public override void OnGUI(Rect position,
                               SerializedProperty property,
                               GUIContent label)
    {
        bool isReadOnly = true;
        if (Application.isPlaying) isReadOnly = (attribute as InspectorReadOnlyAttribute).playing == InspectorReadOnlyAttribute.Mode.ReadOnly;
        else                       isReadOnly = (attribute as InspectorReadOnlyAttribute).editing == InspectorReadOnlyAttribute.Mode.ReadOnly;

        if (isReadOnly) GUI.enabled = false;
        EditorGUI.PropertyField(position, property, label, true);
        if (isReadOnly) GUI.enabled = true;
    }
}