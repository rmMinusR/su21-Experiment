using System.Collections.Generic;
using UnityEngine;

public interface IAnimationProvider
{
    public void WriteAnimations(PlayerAnimationDriver anim);
}

public class PlayerAnimationDriver : MonoBehaviour
{
    [SerializeField] private PlayerHost host;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer playerSprite;

    private BaseMovementAction fallbackSource => host.baseMovement;

    public Facing currentFacing;

    private void Start()
    {
        Debug.Assert(host != null);
        Debug.Assert(animator != null);
        Debug.Assert(playerSprite != null);
    }

    private void Update()
    {
        //Resolve who to poll
        IAnimationProvider casting = host.casting.Owner;
        IAnimationProvider moving = (IAnimationProvider) host.moving.Owner;

        //Poll for new frame data (messy)
        if(casting == null && moving == null)
        {
            fallbackSource.WriteAnimations(this);
        }
        else if(casting == moving)
        {
            if(casting != null) casting.WriteAnimations(this);
        }
        else
        {
            if(casting != null) casting.WriteAnimations(this);
            if(moving  != null) moving .WriteAnimations(this);
        }

        //Process animation buffer
        _currentTimeLeft -= Time.deltaTime;
        if (_currentTimeLeft <= 0)
        {
            if(_buffer.Count > 0)
            {
                _currentlyPlaying = _buffer[0];
                if(_buffer.Count > 1) _buffer.RemoveAt(0);
                //TODO find way that works with name mismatches
                animator.Play(_currentlyPlaying.name);
                _currentTimeLeft = _currentlyPlaying.length;
            }
        }

        //Do sprite flipping
        //Tmp is needed to detect changes
        Facing tmpCurFacing = host.facing;
        if (tmpCurFacing != Facing.Agnostic)
        {
            currentFacing = tmpCurFacing;
            playerSprite.flipX = currentFacing == Facing.Left;
        }
    }

    [Header("Animation status")]
    [SerializeField] private List<AnimationClip> _buffer; //Would use a Queue but it doesn't serialize. Thanks Unity.
    [SerializeField] private AnimationClip _currentlyPlaying; public AnimationClip CurrentlyPlaying => _currentlyPlaying;
    [SerializeField] private float _currentTimeLeft = 0;      public float CurrentTimeLeft => _currentTimeLeft;

    public void PlayAnimation(AnimationClip anim, bool immediately = false)
    {
        if (immediately)
        {
            if (anim != _currentlyPlaying)
            {
                _buffer.Clear();
                _currentTimeLeft = 0;
            }
            else if(_buffer.Count > 1) _buffer.RemoveRange(1, _buffer.Count - 1);
        }
        _buffer.Add(anim);
    }
}