using UnityEngine;

/// <summary>
/// Makes a property read-only in inspector.
/// Taken from https://answers.unity.com/questions/489942/how-to-make-a-readonly-property-in-inspector.html
/// </summary>
public class InspectorReadOnlyAttribute : PropertyAttribute
{
    public bool editMode = true;
    public bool playMode = true;
}