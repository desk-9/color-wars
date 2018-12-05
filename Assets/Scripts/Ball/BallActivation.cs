using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UtilityExtensions;

using EM = EventsManager;
public class BallActivation : MonoBehaviour
{
    // TERMINOLOGY
    // Terminology is loosely modeled after Call of Duty's Capture-the-Flag mode.
    //
    // The CoD state "captured" is replaced with "dominated" to avoid confusion
    // between ball-possession vs. when the team has touched the ball multiple
    // times (so they can score)
    //
    // | Activation      | Fill   | Outer color | Inner color | Corresponding Event    |
    // |-----------------+--------+-------------+-------------+------------------------|
    // | Dominated:Team1 | Filled | Team1       | Team1       | OnBallDominated(Team1) |
    // | Contested:Team1 | Hollow | Team1       | Black       | OnBallContested(Team1) |
    // | Neutral         | Hollow | White       | Black       | OnBallNeutralized      |
    // | Contested:Team2 | Hollow | Team2       | Black       | OnBallContested(Team2) |
    // | Dominated:Team2 | Filled | Team2       | Team2       | OnBallDominated(Team2) |

    private bool isDominated = false;
    public SpriteRenderer innerSpriteRenderer;
    public SpriteRenderer outerSpriteRenderer;
    private Ball ball;

    // These are just here to avoid magic constants. Unfortunately, Colors can't
    // be declared const. But conceptually, these should be const
    private Color neutralOuterColor = Color.white;
    private Color hollowInnerColor = Color.black;

    void Start() {
        // Set up references
        ball = GetComponent<Ball>();
        GameObject inset = gameObject.transform.Find("Inset").gameObject;
        innerSpriteRenderer = inset.GetComponent<SpriteRenderer>();
        outerSpriteRenderer = GetComponent<SpriteRenderer>();

        // Initialize
        ResetBall();
    }

    public void RegisterTouch(BallCarrier ballCarrier) {
        Color playerColor = ballCarrier.team.color;
        Color activationColor = outerSpriteRenderer.color;

        // CASE 1: Player color matches ball
        if (activationColor == playerColor) {
            // Same color, but not yet dominating
            // => dominate for this team
            if (!isDominated) {
                DominateBall(ballCarrier);
            }
            // NOTE: do nothing if the ball is already dominated
            return;
        }

        // CASE 1: Player color does not match ball
        // NOTE: The ball's color is either neutral or the other team's color
        //       Either way, new team is contesting the ball.
        ContestBall(ballCarrier);
    }

    public void ResetBall() {
        isDominated = false;
        innerSpriteRenderer.color = hollowInnerColor;
        outerSpriteRenderer.color = neutralOuterColor;
    }

    private void NeutralizeBall() {
        ResetBall();
        // Fire event
        EM.RaiseOnBallNeutralized();
    }

    private void ContestBall(BallCarrier ballCarrier) {
        Color newColor = ballCarrier.team.color;
        isDominated = false;
        innerSpriteRenderer.color = hollowInnerColor;
        outerSpriteRenderer.color = newColor;
        // Fire event
        var args = new EM.onBallContestedArgs {ballCarrier = ballCarrier, ball = ball};
        EM.RaiseOnBallContested(args);
    }

    private void DominateBall(BallCarrier ballCarrier) {
        Color newColor = ballCarrier.team.color;
        isDominated = true;
        innerSpriteRenderer.color = newColor;
        outerSpriteRenderer.color = newColor;
        // Fire event
        var args = new EM.onBallDominatedArgs {ballCarrier = ballCarrier, ball = ball};
        EM.RaiseOnBallDominated(args);
    }

}
