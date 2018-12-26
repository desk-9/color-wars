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
    private AudioSource backgroundMusic;

    void Start()
    {
        // TODO dkonik: Fix this background music fuckery
        backgroundMusic = GameObject.Find("BGM")?.EnsureComponent<AudioSource>();

        GameManager.NotificationManager.CallOnMessage(Message.BallIsPossessed, HandleBallPossessed, true);
        GameManager.NotificationManager.CallOnMessage(Message.BallIsUnpossessed, HandleBallDropped, true);
        GameManager.NotificationManager.CallOnMessage(Message.GoalScored, SetBackToDefault);
    }

    private void HandleBallPossessed()
    {
        slowMoCount += 1;

        // Only do this if we are just entering slow mo
        if (slowMoCount == 1)
        {
            StartSlowMo();
        }
    }

    private void HandleBallDropped()
    {
        // Ensure slowMo doesn't stop until ALL balls are dropped
        slowMoCount = Mathf.Max(0, slowMoCount - 1);
        if (slowMoCount == 0)
        {
            StopSlowMo();
        }
    }

    private void SetBackToDefault()
    {
        slowMoCount = 0;
        if (pitchShiftCoroutine != null)
        {
            StopCoroutine(pitchShiftCoroutine);
        }
        pitchShiftCoroutine = StartCoroutine(PitchShifter(1.0f, GameManager.Settings.PitchShiftTime));
    }

    private void StartSlowMo()
    {
        Utility.ChangeTimeScale(GameManager.Settings.SlowMoFactor);

        GameManager.NotificationManager.NotifyMessage(Message.SlowMoEntered, this);

        if (pitchShiftCoroutine != null)
        {
            StopCoroutine(pitchShiftCoroutine);
        }
        pitchShiftCoroutine = StartCoroutine(PitchShifter(GameManager.Settings.SlowedPitch, GameManager.Settings.PitchShiftTime));
    }

    private void StopSlowMo()
    {
        Utility.ChangeTimeScale(1);

        // Pitch-shift BGM back to normal.
        if (pitchShiftCoroutine != null)
        {
            StopCoroutine(pitchShiftCoroutine);
        }
        pitchShiftCoroutine = StartCoroutine(PitchShifter(1.0f, GameManager.Settings.PitchShiftTime));
        GameManager.NotificationManager.NotifyMessage(Message.SlowMoExited, this);
    }

    private IEnumerator PitchShifter(float target, float time)
    {
        float start = backgroundMusic.pitch;
        float t = 0.0f;
        while (backgroundMusic.pitch != target && t <= time)
        {
            t += Time.deltaTime;
            backgroundMusic.pitch = Mathf.Lerp(start, target, t / time);
            yield return null;
        }

        backgroundMusic.pitch = target;
        pitchShiftCoroutine = null;
    }
}
