using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SoundName {
    string name;
    public SoundName(string name_) {
        name = name_;
    }

    public static implicit operator SoundName(string s) {
        return new SoundName(s);
    }

    public void Play(float volume = 1) {
        AudioManager.Play(name, volume);
    }
}

public class AudioManager : MonoBehaviour {

    public static AudioManager instance;

    public SoundName GoalSwitchWarning = "GoalSwitchWarning";
    public SoundName GoalSwitch = "GoalSwitch";

    public void Awake() {
        if (instance != null) {
            Destroy(this);
        } else {
            instance = this;
        }
    }

    public static void Play(string name, float volume = 1) {
        AudioClip asset = (AudioClip) Resources.Load("Sounds/" + name, typeof(AudioClip));
        if (asset != null) {
            AudioSource.PlayClipAtPoint(asset, Camera.main.transform.position, volume);
        }
    }
}
