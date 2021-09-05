using System.Collections;
using System.Linq;
using UnityEngine;

public class PlayerAnimationDriver : MonoBehaviour
{
    public MovementController common;
    public Animator animator;
    public SpriteRenderer sprite;

    public Facing currentFacing;

    private void Start()
    {
        Debug.Assert(common != null);
        Debug.Assert(animator != null);
        Debug.Assert(sprite != null);
    }

    private void Update()
    {
        //Removed
        //animator.speed = common.activeMovement.AnimationSpeed;

        AnimatorClipInfo[] currentClips = animator.GetCurrentAnimatorClipInfo(0); //Doesn't play nice with blending...

        //Removed
        //TODO find way that works with name mismatches
        //AnimationClip targetClip = common.activeMovement.CurrentAnimation;
        //if (currentClips.Where(i => i.clip.name == targetClip.name).Count() == 0) animator.Play(targetClip.name);

        //Sprite flipping
        //Tmp is needed to detect changes
        Facing tmpCurFacing = common.activeMovement.currentFacing;
        if (tmpCurFacing != Facing.DontCare)
        {
            currentFacing = tmpCurFacing;
            sprite.flipX = currentFacing == Facing.Left;
        }
    }
}