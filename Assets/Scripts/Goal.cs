using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UtilityExtensions;

public class Goal : MonoBehaviour {

    public TeamManager currentTeam;
    public float goalSwitchInterval = 10;

    IntCallback nextTeamIndex;
    new SpriteRenderer renderer;

    void Awake() {
        renderer = GetComponent<SpriteRenderer>();
    }

    void Start () {
        nextTeamIndex = Utility.ModCycle(0, GameModel.instance.teams.Length);
        SwitchToNextTeam();
        StartCoroutine(TeamSwitching());
    }

    IEnumerator TeamSwitching() {
        while (true) {
            yield return new WaitForSeconds(goalSwitchInterval);
            SwitchToNextTeam();
        }
    }

    TeamManager GetNextTeam() {
        return GameModel.instance.teams[nextTeamIndex()];
    }

    void SwitchToNextTeam() {
        currentTeam = GetNextTeam();
        if (renderer != null) {
            renderer.color = currentTeam.teamColor;
        }
    }

    void ScoreGoal(Ball ball) {
        if (ball.IsOwnable()) {
            ball.ownable = false;
            currentTeam?.IncrementScore();
            // TODO: Non-tweakable placeholder delay on ball reset until it's
            // decided what should happen respawn-wise on goal scoring
            this.TimeDelayCall(ball.ResetBall, 0.35f);
        }
    }

    void OnTriggerEnter2D(Collider2D collider) {
        var ball = collider.gameObject.GetComponent<Ball>();
        if (ball != null) {
            ScoreGoal(ball);
        }
    }
}
