using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class SlowMoManager : MonoBehaviour
{
    public bool IsSlowMo { get { return slowMoCount > 0; } }

    /// <summary>
    /// There may be scenarios or game modes where we have multiple balls.
    /// This ensures that slow mo doesn't stop until all balls are dropped
    /// </summary>
    private int slowMoCount = 0;
    Coroutine pitchShiftCoroutine;

    void Start()
    {
        GameManager.instance.NotificationManager.CallOnMessage(Message.BallIsPossessed, StartSlowMo, true);
        GameManager.instance.NotificationManager.CallOnMessage(Message.BallIsUnpossessed, StopSlowMo, true);
    }

    private void StartSlowMo()
    {
        Utility.ChangeTimeScale(GameManager.Settings.SlowMoFactor);
        slowMoCount += 1;

        // If we just entered slowmo, shift pitch and notify
        if (slowMoCount > 0)
        {
            GameManager.instance.NotificationManager.NotifyMessage(Message.SlowMoEntered, this);

            if (pitchShiftCoroutine != null)
            {
                StopCoroutine(pitchShiftCoroutine);
            }
            pitchShiftCoroutine = StartCoroutine(PitchShifter(GameManager.Settings.SlowedPitch, GameManager.Settings.PitchShiftTime));
        }
    }

    private void StopSlowMo()
    {
        // Ensure slowMo doesn't stop until ALL balls are dropped
        slowMoCount = Mathf.Max(0,slowMoCount - 1);
        if (slowMoCount == 0)
        {
            Utility.ChangeTimeScale(1);

            // Pitch-shift BGM back to normal.
            if (pitchShiftCoroutine != null)
            {
                StopCoroutine(pitchShiftCoroutine);
            }
            pitchShiftCoroutine = StartCoroutine(PitchShifter(1.0f, GameManager.Settings.PitchShiftTime));
            GameManager.instance.NotificationManager.NotifyMessage(Message.SlowMoExited, this);
        }
    }

    private IEnumerator PitchShifter(float target, float time)
    {
        // TODO dkonik: Fix this background music fuckery
        AudioSource backgroundMusic = GameObject.Find("BGM")?.GetComponent<AudioSource>();
        backgroundMusic.ThrowIfNull("Could nto find background music object");

        float start = backgroundMusic.pitch;
        float t = 0.0f;
        while (backgroundMusic.pitch != target && t <= time)
        {
            t += Time.deltaTime;
            backgroundMusic.pitch = Mathf.Lerp(start, target, t / time);
            yield return null;
        }

        backgroundMusic.pitch = target;
    }
}
