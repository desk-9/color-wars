﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UtilityExtensions;
using UnityEngine.UI;

public class Goal : MonoBehaviour {

    public TeamManager currentTeam;
    public float goalSwitchInterval = 10;
    public int goalSwitchNotificationLength = 3;
    public AudioClip goalSwitchWarningAudio;
    public AudioClip goalSwitchAudio;

    IntCallback nextTeamIndex;
    new SpriteRenderer renderer;
    Text goalSwitchText;
    AudioSource audioSource;

    void Awake() {
        renderer = GetComponent<SpriteRenderer>();
    }

    void Start () {
        nextTeamIndex = Utility.ModCycle(0, GameModel.instance.teams.Length);
        goalSwitchText = GetComponentInChildren<Text>();
        audioSource = GetComponent<AudioSource>();
        SwitchToNextTeam();
        StartCoroutine(TeamSwitching());
    }

    void PlayAudioClip(AudioClip play) {
        audioSource.clip = play;
        audioSource.Play();
    }

    void SetNotificationText(string to) {
        if (!goalSwitchText.enabled) {
            goalSwitchText.enabled = true;
        }
        goalSwitchText.text = to;
        PlayAudioClip(goalSwitchWarningAudio);
    }

    IEnumerator TeamSwitching() {
        while (true) {
            yield return new WaitForSeconds(goalSwitchInterval - goalSwitchNotificationLength);
            for (int i = goalSwitchNotificationLength; i > 0; --i) {
                SetNotificationText(i.ToString());
                yield return new WaitForSeconds(1);
            }
            goalSwitchText.enabled = false;
            SwitchToNextTeam(true);
        }
    }

    TeamManager GetNextTeam() {
        return GameModel.instance.teams[nextTeamIndex()];
    }

    void SwitchToNextTeam(bool playSound = false) {
        if (playSound) {
            PlayAudioClip(goalSwitchAudio);
        }
        currentTeam = GetNextTeam();
        if (renderer != null) {
            renderer.color = currentTeam.teamColor;
        }
    }

    void ScoreGoal(Ball ball) {
        if (!ball.IsOwnable()) {
            var stateManager = ball.owner?.GetComponent<PlayerStateManager>();
            if (stateManager != null) {
                stateManager.CurrentStateHasFinished();
            }
        }

        if (ball.IsOwnable()) {
            ball.ownable = false;
            currentTeam?.IncrementScore();
            // TODO: Non-tweakable placeholder delay on ball reset until it's
            // decided what should happen respawn-wise on goal scoring
            this.TimeDelayCall(ball.ResetBall, 0.2f);
        }
    }

    void BallCheck(Collider2D collider) {
        var ball = collider.gameObject.GetComponent<Ball>();
        if (ball != null) {
            ScoreGoal(ball);
        }
    }

    void OnTriggerEnter2D(Collider2D collider) {
        BallCheck(collider);
    }

    void OnTriggerStay2D(Collider2D collider) {
        BallCheck(collider);
    }
}
