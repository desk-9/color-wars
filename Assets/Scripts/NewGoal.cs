using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class NewGoal : MonoBehaviour {

    public TeamManager team {get; private set;}

    new SpriteRenderer renderer;

    public void SetTeam(TeamManager team) {
        this.team = team;
        if (renderer == null) {
            renderer = GetComponent<SpriteRenderer>();
        }
        renderer.color = team.teamColor;
    }

    void OnTriggerEnter2D(Collider2D collider) {
        BallCheck(collider);
    }

    void OnTriggerStay2D(Collider2D collider) {
        BallCheck(collider);
    }

    void BallCheck(Collider2D collider) {
        var ball = collider.gameObject.GetComponent<Ball>();
        if (ball != null) {
            ScoreGoal(ball);
        }
    }

    void ScoreGoal(Ball ball) {
        if (ball.IsOwnable()) {
            ball.ownable = false;
            GameModel.instance.GoalScoredOnTeam(team);
            ball.ResetBall();
        } else {
            var stateManager = ball.owner?.GetComponent<PlayerStateManager>();
            if (stateManager != null) {
                stateManager.CurrentStateHasFinished();
            }
        }
    }

}
