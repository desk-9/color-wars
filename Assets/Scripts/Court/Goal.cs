using UnityEngine;
using UtilityExtensions;
using UnityEngine.UI;

public class Goal : MonoBehaviour
{

    public TeamManager currentTeam;
    public float goalSwitchInterval = 10;
    public int goalSwitchNotificationLength = 3;
    public float goalSwitchWarningVolume = 0.02f;
    public bool timedSwitching = true;
    public bool playerPassSwitching = false;
    public bool respondToSwitchColliders = false;
    public bool resetTimerOnSwitchToSameTeam = false;

    public const float playerNullZoneRadius = 0.1f;
    private ModCycle nextTeamIndex;
    private SpriteRenderer fillRenderer;
    private Text goalSwitchText;
    private Coroutine teamSwitching;
    private Player lastPlayer = null;
    private Color originalColor;

    private GameObject GetPlayerBlocker()
    {
        return transform.Find("PlayerBlocker").gameObject;
    }

    private void BlockBalls()
    {
        GetPlayerBlocker().layer = LayerMask.NameToLayer("Wall");
    }

    private void OnlyBlockPlayers()
    {
        GetPlayerBlocker().layer = LayerMask.NameToLayer("PlayerBlocker");
    }

    private void Awake()
    {
        fillRenderer = transform.FindComponent<SpriteRenderer>("GoalBackground");
        if (fillRenderer != null)
        {
            originalColor = fillRenderer.color;
        }
    }

    public void ResetNeutral()
    {
        SwitchToNextTeam(false);
        currentTeam = null;
        lastPlayer = null;
        BlockBalls();
        if (fillRenderer != null)
        {
            fillRenderer.color = originalColor;
        }
    }

    private void Start()
    {
        //originalColor = renderer.color;
        nextTeamIndex = new ModCycle(0, GameManager.instance.teams.Count);
        GameManager.instance.OnGameOver += StopTeamSwitching;
        ResetNeutral();
        RestartTeamSwitching();
        if (playerPassSwitching)
        {
            RegisterPassSwitching();
        }
        GameManager.instance.notificationManager.CallOnStringEventWithSender(
            GoalSwitchCollider.EventId, ColliderSwitch);
    }

    public void RestartTeamSwitching()
    {
        // if (teamSwitching != null) {
        //     SetNotificationText("");
        //     StopCoroutine(teamSwitching);
        // }
        // teamSwitching = StartCoroutine(TeamSwitching());
    }

    private void RegisterPassSwitching()
    {
        GameManager.instance.notificationManager.CallOnStateEnd(
            State.Posession, (Player player) => lastPlayer = player);
        GameManager.instance.notificationManager.CallOnStateStart(
            State.Posession, (Player player) => PlayerBallColorSwitch(player));
    }

    private bool PlayerInNullZone(Player player, float radius = playerNullZoneRadius)
    {
        Collider2D collider = Physics2D.OverlapCircle(
            player.transform.position, radius, LayerMask.GetMask("NullZone"));
        return collider != null;
    }

    private void PlayerBallColorSwitch(Player player)
    {
        if (this == null)
        {
            return;
        }
        if (player != lastPlayer && player.team == lastPlayer?.team)
        {
            if (!PlayerInNullZone(player))
            {
                GameManager.instance.notificationManager.NotifyMessagePlayer(Message.BallCharged, player);
                SwitchToTeam(player.team);
            }
            else
            {
                if (currentTeam == null)
                {
                    GameManager.instance.notificationManager.NotifyMessagePlayer(Message.NullChargePrevention, player);
                    AudioManager.instance.PassToNullZone.Play(.1f);
                }
            }
        }
        else if (player.team != lastPlayer?.team)
        {
            GameManager.instance.notificationManager.NotifyMessagePlayer(Message.BallSetNeutral, player);
            ResetNeutral();
        }
        if (currentTeam == null)
        {
            GameManager.instance.notificationManager.NotifyMessagePlayer(Message.BallPossessedWhileNeutral, player);
        }
        else
        {
            GameManager.instance.notificationManager.NotifyMessagePlayer(Message.BallPossessedWhileCharged, player);
        }
    }

    private void SwitchToTeam(TeamManager team)
    {
        if (team == null)
        {
            Debug.LogWarning("Team in SwitchToTeam is null");
            return;
        }
        if (resetTimerOnSwitchToSameTeam && team == currentTeam)
        {
            RestartTeamSwitching();
        }
        if (currentTeam != team)
        {
            AudioManager.instance.GoalSwitch.Play();
        }
        while (currentTeam != team)
        {
            SwitchToNextTeam();
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
            // Utility.TutEvent("Backboard", ball.lastOwner);
            TeamManager ballTeam = ball.LastOwner?.GetComponent<Player>()?.team;
            SwitchToTeam(ballTeam);
        }
    }

    public void StopTeamSwitching()
    {
        // if (teamSwitching != null) {
        //     StopCoroutine(teamSwitching);
        //     teamSwitching = null;
        //     SetNotificationText("", false);
        // }
    }

    private TeamManager PeekNextTeam()
    {
        return GameManager.instance.teams[nextTeamIndex.PeekNext()];
    }

    private TeamManager GetNextTeam()
    {
        return GameManager.instance.teams[nextTeamIndex.Next()];
    }

    public void SwitchToNextTeam(bool playSound = false)
    {
        if (playSound)
        {
            AudioManager.instance.GoalSwitch.Play();
        }
        currentTeam = GetNextTeam();
        OnlyBlockPlayers();
        if (fillRenderer != null)
        {
            fillRenderer.color = currentTeam.teamColor;
        }
        RestartTeamSwitching();
    }

    private void ScoreGoal(Ball ball)
    {
        if (currentTeam != null)
        {
            GameManager.instance.GoalScoredForTeam(currentTeam);
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
