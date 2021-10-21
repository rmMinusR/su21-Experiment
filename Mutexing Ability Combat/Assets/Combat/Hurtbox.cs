using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Hurtbox : MonoBehaviour
{
    public IDamageable owner;

    private void Awake()
    {
        if (owner == null) owner = GetComponentInParent<IDamageable>();
        Debug.Assert(owner != null);
    }
}