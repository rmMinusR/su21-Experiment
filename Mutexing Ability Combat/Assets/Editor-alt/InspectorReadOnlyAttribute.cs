using UnityEngine;

/*
 * 
 * Makes a property read-only in inspector
 * Taken from https://answers.unity.com/questions/489942/how-to-make-a-readonly-property-in-inspector.html
 * 
 */

public class InspectorReadOnlyAttribute : PropertyAttribute
{
    public Mode editing;
    public Mode playing;

    public InspectorReadOnlyAttribute(Mode editing = Mode.ReadOnly, Mode playing = Mode.ReadOnly)
    {
        this.editing = editing;
        this.playing = playing;
    }

    public enum Mode
    {
        ReadOnly,
        ReadWrite
    }
}
