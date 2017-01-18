using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour {
    //audio
    public List<AudioClip> AudioClips;
    private AudioSource _backMusicSource = null;
    private AudioSource _SoundSource = null;

    // Use this for initialization
    void Start () {
        _backMusicSource = gameObject.AddComponent<AudioSource>();
        _backMusicSource.loop = true;

        _SoundSource = gameObject.AddComponent<AudioSource>();
        _SoundSource.volume = 0.48f;
        //PlayMusic("Music/test");
        //PlaySound("Music/test1");

        _backMusicSource.clip = this.AudioClips[0];
        _backMusicSource.Play();
    }

    private void PlayMusic(string musicPath)
    {
        AudioClip clip = Resources.Load(musicPath) as AudioClip;
        _backMusicSource.clip = clip;
        _backMusicSource.Play();
    }

    private void PlaySound(string soundPath)
    {
        AudioClip clip = Resources.Load(soundPath) as AudioClip;
        _SoundSource.PlayOneShot(clip);
    }

    // Update is called once per frame
    void Update () {
	    //...
	}

    public void PlayOneShotIndex(int i)
    {
        if (i == 0)
        {
            //DebugHelper.Assert(false);
            Debug.LogError("Invalid Play Shot: " + i);
        }

        if (i < 0 || i >= this.AudioClips.Count)
        {
            Debug.LogError("Play Shot over range: " + i);
            return;
        }

        _SoundSource.PlayOneShot(this.AudioClips[i]);
    }
}
