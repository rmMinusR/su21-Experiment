using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedDestroy : MonoBehaviour
{
    [SerializeField] [Min(0)] private float lifetimeTotal;
    [SerializeField] [InspectorReadOnly(playing = InspectorReadOnlyAttribute.Mode.ReadWrite)] private float lifetimeRemaining;
    
    void Awake()
    {
        lifetimeRemaining = lifetimeTotal;
    }
    
    void Update()
    {
        lifetimeRemaining -= Time.deltaTime;
        if (lifetimeRemaining < 0) Destroy(gameObject);
    }
}
