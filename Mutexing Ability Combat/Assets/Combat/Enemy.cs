using Events;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Combatant
{
    public override bool ShowHealthUI()
    {
        return false; //TODO
    }

    public override string GetDisplayName() => "Enemy";
}