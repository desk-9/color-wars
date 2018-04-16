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

    public const float playerNullZoneRadius = 0.1f;

    ModCycle nextTeamIndex;
    SpriteRenderer fillRenderer;
    Text goalSwitchText;
    Coroutine teamSwitching;
    Player lastPlayer = null;
    Color originalColor;

    GameObject GetPlayerBlocker() {
        return transform.Find("PlayerBlocker").gameObject;
    }

    void BlockBalls() {
        GetPlayerBlocker().layer = LayerMask.NameToLayer("Wall");
    }

    void OnlyBlockPlayers() {
        GetPlayerBlocker().layer = LayerMask.NameToLayer("PlayerBlocker");
    }

    void Awake() {
        fillRenderer = transform.FindComponent<SpriteRenderer>("GoalBackground");
        if (fillRenderer != null) {
            originalColor = fillRenderer.color;
        }
    }

    public void ResetNeutral() {
        SwitchToNextTeam(false);
        currentTeam = null;
        lastPlayer = null;
        BlockBalls();
        if (fillRenderer != null) {
            fillRenderer.color = originalColor;
        }
    }

    void Start () {
        //originalColor = renderer.color;
        nextTeamIndex = new ModCycle(0, GameModel.instance.teams.Count);
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
        // if (teamSwitching != null) {
        //     SetNotificationText("");
        //     StopCoroutine(teamSwitching);
        // }
        // teamSwitching = StartCoroutine(TeamSwitching());
    }


    void RegisterPassSwitching() {
        GameModel.instance.nc.CallOnStateEnd(
            State.Posession, (Player player) => lastPlayer = player);
        GameModel.instance.nc.CallOnStateStart(
            State.Posession, (Player player) => PlayerBallColorSwitch(player));
    }

    bool PlayerInNullZone(Player player, float radius = playerNullZoneRadius) {
        var collider = Physics2D.OverlapCircle(
            player.transform.position, radius, LayerMask.GetMask("NullZone"));
        return collider != null;
    }

    void PlayerBallColorSwitch(Player player) {
        if (this == null) {
            return;
        }
        if (player != lastPlayer && player.team == lastPlayer?.team) {
            if (!PlayerInNullZone(player)) {
                GameModel.instance.nc.NotifyMessage(Message.BallCharged, player);
                SwitchToTeam(player.team);
            } else {
                if (currentTeam == null) {
                        GameModel.instance.nc.NotifyMessage(Message.NullChargePrevention, player);
                        AudioManager.instance.PassToNullZone.Play(.1f);
                    }
            }
        } else if (player.team != lastPlayer?.team) {
            GameModel.instance.nc.NotifyMessage(Message.BallSetNeutral, player);
            ResetNeutral();
        }
        if (currentTeam == null) {
            GameModel.instance.nc.NotifyMessage(Message.BallPossessedWhileNeutral, player);
        } else {
            GameModel.instance.nc.NotifyMessage(Message.BallPossessedWhileCharged, player);
        }
    }

    void SwitchToTeam(TeamManager team) {
        if (team == null) {
            Debug.LogWarning("Team in SwitchToTeam is null");
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
            // Utility.TutEvent("Backboard", ball.lastOwner);
            var ballTeam = ball.lastOwner?.GetComponent<Player>()?.team;
            SwitchToTeam(ballTeam);
        }
    }

    public void StopTeamSwitching() {
        // if (teamSwitching != null) {
        //     StopCoroutine(teamSwitching);
        //     teamSwitching = null;
        //     SetNotificationText("", false);
        // }
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
        currentTeam = GetNextTeam();
        OnlyBlockPlayers();
        if (fillRenderer != null) {
            fillRenderer.color = currentTeam.teamColor;
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
            if (currentTeam != null) {
                GameModel.instance.GoalScoredForTeam(currentTeam);
            }
        }
    }

    void BallCheck(GameObject thing) {
        var ball = thing.gameObject.GetComponent<Ball>();
        if (ball != null) {
            ScoreGoal(ball);
            this.FrameDelayCall(() => AudioManager.instance.ScoreGoalSound.Play(0.75f), 10);
        }
    }

    void OnTriggerEnter2D(Collider2D collider) {
        BallCheck(collider.gameObject);
    }
}
