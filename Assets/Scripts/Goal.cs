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
    public bool timedSwitching = true;
    public bool playerPassSwitching = false;
    public bool respondToSwitchColliders = false;
    public bool resetTimerOnSwitchToSameTeam = false;

    ModCycle nextTeamIndex;
    new SpriteRenderer renderer;
    Text goalSwitchText;
    Coroutine teamSwitching;

    Color originalColor;

    GameObject GetPlayerBlocker() {
        return transform.Find("PlayerBlocker").gameObject;
    }

    void BlockBalls() {
        GetPlayerBlocker().layer = LayerMask.NameToLayer("Default");
    }

    void OnlyBlockPlayers() {
        GetPlayerBlocker().layer = LayerMask.NameToLayer("PlayerBlocker");
    }

    void Awake() {
        renderer = GetComponent<SpriteRenderer>();
    }

    public void ResetNeutral() {
        BlockBalls();
        renderer.color = originalColor;
    }

    void Start () {
        originalColor = renderer.color;
        nextTeamIndex = new ModCycle(0, GameModel.instance.teams.Length);
        goalSwitchText = GetComponentInChildren<Text>();
        GameModel.instance.OnGameOver += StopTeamSwitching;
        ResetNeutral();
        RestartTeamSwitching();
        if (playerPassSwitching) {
            RegisterPassSwitching();
        }
        GameModel.instance.nc.CallOnStringEventWithSender(
            GoalSwitchCollider.EventId, ColliderSwitch);
    }

    public void RestartTeamSwitching() {
        if (teamSwitching != null) {
            SetNotificationText("");
            StopCoroutine(teamSwitching);
        }
        teamSwitching = StartCoroutine(TeamSwitching());
    }

    void RegisterPassSwitching() {
        Player last_player = null;
        GameModel.instance.nc.CallOnStateEnd(
            State.Posession, (Player player) => last_player = player);
        GameModel.instance.nc.CallOnStateStart(
            State.Posession, (Player player) => {
                if (player != last_player && player.team == last_player?.team) {
                    SwitchToTeam(player.team);
                }
            });
    }

    void SwitchToTeam(TeamManager team) {
        if (team == null) {
            Debug.Log("Team in SwitchToTeam is null");
            return;
        }
        if (resetTimerOnSwitchToSameTeam && team == currentTeam) {
            RestartTeamSwitching();
        }
        if (currentTeam != team) {
            AudioManager.instance.GoalSwitch.Play();
        }
        while (currentTeam != team) {
            SwitchToNextTeam();
        }
    }

    void ColliderSwitch(object thing) {
        if (!respondToSwitchColliders) {
            return;
        }
        var gameThing = (GameObject) thing;
        var ball = gameThing.GetComponent<Ball>();
        if (ball != null) {
            Utility.TutEvent("Backboard", this);
            var ballTeam = ball.lastOwner?.GetComponent<Player>()?.team;
            SwitchToTeam(ballTeam);
        }
    }

    void SetNotificationText(string to, bool playSound = true) {
        if (!goalSwitchText.enabled) {
            goalSwitchText.enabled = true;
        }
        goalSwitchText.text = to;
        if (playSound) {
            AudioManager.instance.GoalSwitchWarning.Play(goalSwitchWarningVolume);
        }
    }

    IEnumerator TeamSwitching() {

        yield return new WaitForSeconds(goalSwitchInterval - goalSwitchNotificationLength);
        if (!timedSwitching) {
            yield break;
        }
        goalSwitchText.color = PeekNextTeam().teamColor;
        for (int i = goalSwitchNotificationLength; i > 0; --i) {
            SetNotificationText(i.ToString());
            yield return new WaitForSeconds(1);
        }
        goalSwitchText.enabled = false;
        SwitchToNextTeam(true);
    }

    public void StopTeamSwitching() {
        if (teamSwitching != null) {
            StopCoroutine(teamSwitching);
            teamSwitching = null;
            SetNotificationText("", false);
        }
    }


    TeamManager PeekNextTeam() {
        return GameModel.instance.teams[nextTeamIndex.PeekNext()];
    }

    TeamManager GetNextTeam() {
        return GameModel.instance.teams[nextTeamIndex.Next()];
    }

    public void SwitchToNextTeam(bool playSound = false) {
        if (playSound) {
            AudioManager.instance.GoalSwitch.Play();
        }
        OnlyBlockPlayers();
        currentTeam = GetNextTeam();
        if (renderer != null) {
            renderer.color = currentTeam.teamColor;
        }
        RestartTeamSwitching();
    }

    void ScoreGoal(Ball ball) {
        if (!ball.IsOwnable()) {
            var stateManager = ball.owner?.GetComponent<PlayerStateManager>();
            if (stateManager != null) {
                stateManager.CurrentStateHasFinished();
            }
        }

        if (ball.IsOwnable()) {
            if (ball.lastOwner?.GetComponent<Player>()?.team == currentTeam) {
                Utility.TutEvent("Score", this);
            }
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
