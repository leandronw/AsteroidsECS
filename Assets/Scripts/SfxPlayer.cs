using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SfxPlayer : MonoBehaviour
{
    public static SfxPlayer Instance { get; private set; } // singleton instance

    [SerializeField] private SoundClipReference[] _clips;

    private AudioSource _oneShotAudioSource;
    private Dictionary<SoundId, AudioSource> _loopingAudioSources;
    private Dictionary<SoundId, AudioClip> _clipsDictionary;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        _oneShotAudioSource = GetComponent<AudioSource>();
        _loopingAudioSources = new Dictionary<SoundId, AudioSource>();

        _clipsDictionary = new Dictionary<SoundId, AudioClip>();
        foreach (SoundClipReference reference in _clips)
        {
            _clipsDictionary.Add(reference.Id, reference.Clip);
        }
    }

    public void PlaySound(SoundId soundId)
    {
        _oneShotAudioSource.PlayOneShot(_clipsDictionary[soundId]);
    }

    public void PlayLoop(SoundId soundId)
    {
        if (!_loopingAudioSources.ContainsKey(soundId))
        {
            AudioSource newAudioSource = gameObject.AddComponent<AudioSource>();
            newAudioSource.playOnAwake = false;
            newAudioSource.clip = _clipsDictionary[soundId];
            newAudioSource.loop = true;
            _loopingAudioSources.Add(soundId, newAudioSource);
        }

        AudioSource audioSouce = _loopingAudioSources[soundId];
        if (!audioSouce.isPlaying)
        {
            _loopingAudioSources[soundId].Play();
        }   
    }

    public void StopLoop(SoundId soundId)
    {
        if (!_loopingAudioSources.ContainsKey(soundId))
        {
            return;
        }

        _loopingAudioSources[soundId].Stop();
    }
}
