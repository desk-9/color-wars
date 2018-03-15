using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioManager {
    public static void Play(string name, float volume = 1) {
        AudioClip asset = (AudioClip) Resources.Load("Sounds/" + name, typeof(AudioClip));
        if (asset != null) {
            AudioSource.PlayClipAtPoint(asset, Camera.main.transform.position, volume);
        }
    }
}

