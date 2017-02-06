using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class pvp_audio : MonoBehaviour {
    //audio
    public List<AudioClip> AudioClips;
    private AudioSource _SoundSource = null;

    // Use this for initialization
    void Start () {
        _SoundSource = gameObject.AddComponent<AudioSource>();
        _SoundSource.volume = 0.48f;
    }
	
	// Update is called once per frame
	void Update () {
	    //...
	}

    public void PlayOneShotIndex(int i)
    {
        if (i < 0 || i >= this.AudioClips.Count)
        {
            Debug.LogError("Play Shot over range: " + i);
            return;
        }

        _SoundSource.PlayOneShot(this.AudioClips[i]);
    }
}
