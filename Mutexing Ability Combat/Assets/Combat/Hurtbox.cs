using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Hurtbox : MonoBehaviour
{
    public IDamageable owner;

    private void Awake()
    {
        Debug.Assert(owner != null);
    }
}