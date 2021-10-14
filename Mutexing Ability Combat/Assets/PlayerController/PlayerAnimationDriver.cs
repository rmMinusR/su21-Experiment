using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationDriver : MonoBehaviour
{
    [SerializeField] private PlayerHost context;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer playerSprite;

    public Facing currentFacing;

    private void Start()
    {
        Debug.Assert(context != null);
        Debug.Assert(animator != null);
        Debug.Assert(playerSprite != null);
    }

    private void Update()
    {
        //Process animation buffer
        currentTimeLeft -= Time.deltaTime;
        if (currentTimeLeft <= 0)
        {
            if(buffer.Count > 0)
            {
                currentlyPlaying = buffer[0];
                if(buffer.Count > 1) buffer.RemoveAt(0);
                //TODO find way that works with name mismatches
                animator.Play(currentlyPlaying.name);
                currentTimeLeft = currentlyPlaying.length;
            }
        }

        //Do sprite flipping
        //Tmp is needed to detect changes
        Facing tmpCurFacing = context.facing;
        if (tmpCurFacing != Facing.Agnostic)
        {
            currentFacing = tmpCurFacing;
            playerSprite.flipX = currentFacing == Facing.Left;
        }
    }

    [Header("Animation status")]
    [SerializeField] private List<AnimationClip> buffer; //Would use a Queue but it doesn't serialize. Thanks Unity.
    [SerializeField] private AnimationClip currentlyPlaying; public AnimationClip CurrentlyPlaying => currentlyPlaying;
    [SerializeField] private float currentTimeLeft = 0;

    public void PlayAnimation(AnimationClip anim, bool immediately = false)
    {
        if (immediately)
        {
            if (anim != currentlyPlaying)
            {
                buffer.Clear();
                currentTimeLeft = 0;
            }
            else if(buffer.Count > 1) buffer.RemoveRange(1, buffer.Count - 1);
        }
        buffer.Add(anim);
    }
}