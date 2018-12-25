using UnityEngine;
using UtilityExtensions;
using UnityEngine.UI;

public class Goal : MonoBehaviour
{
    public bool respondToSwitchColliders = false;

    private SpriteRenderer fillRenderer;
    private Color originalColor;
    private GameObject playerBlocker;

    private void BlockBalls()
    {
        playerBlocker.layer = LayerMask.NameToLayer("Wall");
    }

    private void OnlyBlockPlayers()
    {
        playerBlocker.layer = LayerMask.NameToLayer("PlayerBlocker");
    }

    private void Awake()
    {
        fillRenderer = transform.FindComponent<SpriteRenderer>("GoalBackground")
            .ThrowIfNull("Could not find goal background renderer");
        originalColor = fillRenderer.color;
    }

    private void ResetNeutral()
    {
        BlockBalls();
        fillRenderer.color = originalColor;
    }

    private void Start()
    {
        playerBlocker = transform.Find("PlayerBlocker").gameObject
            .ThrowIfNull("Could not find player blocker");
        ResetNeutral();

        NotificationManager notificationManager = GameManager.NotificationManager;
        notificationManager.CallOnStringEventWithSender(GoalSwitchCollider.EventId, ColliderSwitch);
        notificationManager.CallOnMessage(Message.ChargeChanged, HandleChargeChanged);
        notificationManager.CallOnMessage(Message.ResetAfterGoal, ResetNeutral);
    }

    private void HandleChargeChanged()
    {
        if (GameManager.PossessionManager.IsCharged)
        {
            OnlyBlockPlayers();
            if (fillRenderer != null)
            {
                fillRenderer.color = GameManager.PossessionManager.CurrentTeam.TeamColor;
            }
            AudioManager.instance.GoalSwitch.Play();
        } else
        {
            ResetNeutral();
        }
    }

    private void ColliderSwitch(object thing)
    {
        if (!respondToSwitchColliders)
        {
            return;
        }
        GameObject gameThing = (GameObject)thing;
        Ball ball = gameThing.GetComponent<Ball>();
        if (ball != null)
        {
            TeamManager ballTeam = ball.LastOwner?.GetComponent<Player>()?.Team;
        }
    }

    private void ScoreGoal(Ball ball)
    {
        TeamManager currentTeam = GameManager.PossessionManager.CurrentTeam;
        if (currentTeam != null)
        {
            GameManager.NotificationManager.NotifyMessage(Message.GoalScored, this);
        } else
        {
            Debug.LogError("Team scored goal but PossessionManager.CurrentTeam is null");
        }
    }

    private void BallCheck(GameObject thing)
    {
        // TODO dkonik: We realllllly shouldn't be relying on the ball to know whether or not
        // a goal is allowed to be scored. There should be some check in GameManager
        // that designates that, based on the current state of the game.

        Ball ball = thing.gameObject.GetComponent<Ball>();
        // Need to check that ball.ownable (*not* ball.IsOwnable) here
        // Otherwise, the body of this if statement is executed every time the
        // ball enters the goal (even after a goal is scored!) Yikes!
        // Right now (Monday, apr 16 2:35am), the semantics of
        // ball.ownable are seen in Ball.cs functions ResetBall and HandleGoalScore
        if (ball != null && ball.Ownable)
        {
            ScoreGoal(ball);
            this.FrameDelayCall(() => AudioManager.instance.ScoreGoalSound.Play(0.75f), 10);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        BallCheck(collider.gameObject);
    }
}
