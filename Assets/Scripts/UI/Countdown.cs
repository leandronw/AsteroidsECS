using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Countdown : MonoBehaviour
{
    private Animator _animator;

    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
    }

    public void Play(float time)
    {
        // turn off and on so animator resets
        this.gameObject.SetActive(false);
        this.gameObject.SetActive(true);
        _animator.SetFloat("Speed", 1f / time);
    }

    // called from animation
    public void ANIM_PlayCountdownSound()
    {
        SfxPlayer.Instance.PlaySound(SoundId.COUNTDOWN);
    }
}
