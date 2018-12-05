using UnityEngine;
using UtilityExtensions;
using UnityEngine.UI;

using EM = EventsManager;
// TODO: set layer of Game Object Goal > PlayerBlocker to 'Wall'
public class Goal : MonoBehaviour
{

    public TeamManager currentTeam = null;
    private bool canScore() {return currentTeam != null;}
    private SpriteRenderer goalBackgroundRenderer;
    private Color neutralGoalColor;

    private void Start()
    {
        goalBackgroundRenderer = transform.FindComponent<SpriteRenderer>("GoalBackground");
        Debug.Assert(goalBackgroundRenderer != null,
                     "`Goal` must have child `GoalBackground` w/SpriteRenderer!");
        neutralGoalColor = goalBackgroundRenderer.color;
        EM.onBallDominated += this.SetGoalColor;
        EM.onBallNeutralized += this.ResetNeutral;
        EM.onResetAfterGoal += this.ResetNeutral;
    }

    public void ResetNeutral()
    {
        currentTeam = null;
        goalBackgroundRenderer.color = neutralGoalColor;
    }

    public void SetGoalColor(EM.onBallDominatedArgs args) {
        var ballCarrier = args.ballCarrier;
        var ball = args.ball;

        currentTeam = ballCarrier.team;
        Debug.Assert(currentTeam != null,
                     "Tried to set goal color to match null team!");
        goalBackgroundRenderer.color = ballCarrier.team.color;
    }

    private void Score(Ball ball)
    {
        Debug.Assert(currentTeam != null,
                     "Tried to score goal, but current team is null!");
        GameManager.instance.GoalScoredForTeam(currentTeam);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        Ball ball = collider.gameObject.GetComponent<Ball>();
        if (ball != null && canScore()) {
            Score(ball);
        }
    }
}
