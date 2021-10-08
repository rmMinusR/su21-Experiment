using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbOverride : MonoBehaviour
{
    public static bool Process(bool canNormallyClimb, GameObject which)
    {
        ClimbOverride @override = which.GetComponent<ClimbOverride>();
        if(@override == null) return canNormallyClimb;

        switch (@override.mode)
        {
            case Mode.Normal: return canNormallyClimb;
            case Mode.AlwaysClimbable: return true;
            case Mode.NeverClimbable: return false;
            default: throw new NotImplementedException();
        }
    }

    public enum Mode
    {
        Normal,
        AlwaysClimbable,
        NeverClimbable
    }

    public Mode mode;
}
