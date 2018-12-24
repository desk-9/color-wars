using UnityEngine;
using UtilityExtensions;

public class SoundName
{
    public bool shouldSlowMo = false;
    private string name;
    private AudioClip asset = null;

    public SoundName(string name, AudioClip asset = null, bool shouldSlowMo = false)
    {
        this.name = name;
        this.asset = asset;
        this.shouldSlowMo = shouldSlowMo;
    }

    public static implicit operator SoundName(string s)
    {
        return new SoundName(s);
    }

    public static implicit operator string(SoundName s)
    {
        return s.name;
    }

    public void Play(float volume = 1)
    {
        if (asset == null)
        {
            asset = AudioManager.LoadAsset(name);
        }
        if (Camera.main != null)
        {
            AudioManager.instance.PlayClip(asset, volume, shouldSlowMo);
        }
    }

    public float Length()
    {
        if (asset != null)
        {
            return asset.length;
        }
        return 0.0f;
    }
}

public class AudioManager : MonoBehaviour
{

    public static AudioManager instance;
    private AudioSource source;

    public SoundName GoalSwitchWarning = "Beep";
    public SoundName Beep = "Beep";
    public SoundName Ching = "Ching";
    public SoundName GoalSwitch = "GoalSwitch";
    public SoundName StealSound = "StealSoundEffect";
    public SoundName ShootBallSound = "ShootBallSound";
    public SoundName PowerUpBall = "PoweringUpBall";
    public SoundName StunPlayerWallBreak = "StunPlayerWalBreak";
    public SoundName BreakWall = "BreakWall";
    public SoundName ScoreGoalSound = "ScoreGoalSound";
    public SoundName DashSound = new SoundName("DashSound", shouldSlowMo: true);
    public SoundName PassToNullZone = "PassToNullZone";

    // Menu sounds
    public SoundName PauseSound = "PauseSound";
    public SoundName UnPauseSound = "UnPauseSound";
    public SoundName ConfirmSelectionSound = "ConfirmSelectionSound";
    public SoundName CheatCodeSound = "CheatCodeSound";

    public void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

    private void Start()
    {
        source = Camera.main.gameObject.AddComponent<AudioSource>();
    }

    public void PlayClip(AudioClip asset, float volume, bool shouldSlowMo = false)
    {
        if (shouldSlowMo && GameManager.Instance != null
            && GameManager.Settings.RespectSoundEffectSlowMo
            && GameManager.Instance.SlowMoManager.IsSlowMo)
        {

            source.pitch = GameManager.Settings.SlowedPitch;
            source.PlayOneShot(asset, volume);
            this.RealtimeDelayCall(() => source.pitch = 1, asset.length);
        }
        else
        {
            source.PlayOneShot(asset, volume);
        }
    }

    public static AudioClip LoadAsset(string name)
    {
        AudioClip asset = (AudioClip)Resources.Load("Sounds/" + name, typeof(AudioClip));
        return asset;
    }

    public static void Play(string name, float volume = 1)
    {
        AudioClip asset = (AudioClip)Resources.Load("Sounds/" + name, typeof(AudioClip));
        if (asset != null)
        {
            AudioSource.PlayClipAtPoint(asset, Camera.main.transform.position, volume);
        }
    }

    public static float Length(string name)
    {
        AudioClip asset = (AudioClip)Resources.Load("Sounds/" + name, typeof(AudioClip));
        if (asset != null)
        {
            return asset.length;
        }
        return 0.0f;
    }
}
