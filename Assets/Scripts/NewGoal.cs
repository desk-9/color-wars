using UnityEngine;

public class NewGoal : MonoBehaviour
{

    public TeamManager team { get; private set; }

    private new SpriteRenderer renderer;

    public void SetTeam(TeamManager team)
    {
        this.team = team;
        if (renderer == null)
        {
            renderer = GetComponent<SpriteRenderer>();
        }
        renderer.color = team.teamColor;
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        BallCheck(collider);
    }

    private void OnTriggerStay2D(Collider2D collider)
    {
        BallCheck(collider);
    }

    private void BallCheck(Collider2D collider)
    {
        Ball ball = collider.gameObject.GetComponent<Ball>();
        if (ball != null)
        {
            ScoreGoal(ball);
        }
    }

    private void ScoreGoal(Ball ball)
    {
        if (ball.IsOwnable())
        {
            ball.Ownable = false;
            GameModel.instance.GoalScoredOnTeam(team);
            ball.ResetBall();
        }
        else
        {
            PlayerStateManager stateManager = ball.Owner?.GetComponent<PlayerStateManager>();
            if (stateManager != null)
            {
                stateManager.CurrentStateHasFinished();
            }
        }
    }

}
