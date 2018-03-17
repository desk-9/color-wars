using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UtilityExtensions;
using UnityEngine.UI;

public class Goal : MonoBehaviour {

    public TeamManager currentTeam;
    public float goalSwitchInterval = 10;
    public int goalSwitchNotificationLength = 3;
    public float goalSwitchWarningVolume = 0.02f;


    ModCycle nextTeamIndex;
    new SpriteRenderer renderer;
    Text goalSwitchText;

    void Awake() {
        renderer = GetComponent<SpriteRenderer>();
    }

    void Start () {
        nextTeamIndex = new ModCycle(0, GameModel.instance.teams.Length);
        goalSwitchText = GetComponentInChildren<Text>();
        SwitchToNextTeam();
        StartCoroutine(TeamSwitching());
    }

    void SetNotificationText(string to) {
        if (!goalSwitchText.enabled) {
            goalSwitchText.enabled = true;
        }
        goalSwitchText.text = to;
        AudioManager.instance.GoalSwitchWarning.Play(goalSwitchWarningVolume);
    }

    IEnumerator TeamSwitching() {
        while (true) {
            yield return new WaitForSeconds(goalSwitchInterval - goalSwitchNotificationLength);
            goalSwitchText.color = PeekNextTeam().teamColor;
            for (int i = goalSwitchNotificationLength; i > 0; --i) {
                SetNotificationText(i.ToString());
                yield return new WaitForSeconds(1);
            }
            goalSwitchText.enabled = false;
            SwitchToNextTeam(true);
        }
    }

    TeamManager PeekNextTeam() {
        return GameModel.instance.teams[nextTeamIndex.PeekNext()];
    }

    TeamManager GetNextTeam() {
        return GameModel.instance.teams[nextTeamIndex.Next()];
    }

    void SwitchToNextTeam(bool playSound = false) {
        if (playSound) {
            AudioManager.instance.GoalSwitch.Play();
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
            GameModel.instance.GoalScoredForTeam(currentTeam);
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
