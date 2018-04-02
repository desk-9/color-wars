using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SoundName {
    string name;
    public SoundName(string name) {
        this.name = name;
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

    public SoundName GoalSwitchWarning = "Beep";
    public SoundName Beep = "Beep";
    public SoundName GoalSwitch = "GoalSwitch";
    public SoundName StealSound = "StealSoundEffect";
    public SoundName ShootBallSound = "BallShoot";
    public SoundName PowerUpBall = "PoweringUpBall";
    public SoundName StunPlayerWallBreak = "StunPlayerWalBreak";
    public SoundName BreakWall = "BreakWall";
    public SoundName ScoreGoalSound = "ScoreGoalSound";
    public SoundName DashSound = "DashSound";

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
