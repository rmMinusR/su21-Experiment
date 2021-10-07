using System;
using UnityEngine;
using UnityEngine.UI;

public class TextMessage : MonoBehaviour
{
    public float lifetime;
    [SerializeField] private float timeRemaining;
    [SerializeField] private Vector2 posDelta;

    public Text uiText;

    private void Start()
    {
        timeRemaining = lifetime;
    }

    private void Update()
    {
        //Move
        transform.position += (Vector3)posDelta * Time.deltaTime;

        //Fade out
        Color col = uiText.color;
        col.a = timeRemaining / lifetime;
        uiText.color = col;

        //Tick time
        timeRemaining -= Time.deltaTime;
        if(timeRemaining < 0) Destroy(gameObject);
    }
}
