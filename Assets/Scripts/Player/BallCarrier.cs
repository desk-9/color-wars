using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class BallCarrier : MonoBehaviour
{
    public GameObject blowbackEffectPrefab;
    public float coolDownTime = .1f;
    public Ball Ball { private set; get; }

    private float ballTurnSpeed = 10f;
    public bool slowMoOnCarry = true;
    
    public float timeCarryStarted { get; private set; }
    public float blowbackRadius = 3f;
    public float blowbackForce = 5f;
    public float blowbackStunTime = 0.1f;
    private float ballOffsetFromCenter = .5f;
    private PlayerMovement playerMovement;
    private PlayerStateManager stateManager;
    private Coroutine carryBallCoroutine;
    private bool isCoolingDown = false;
    private LaserGuide laserGuide;
    private GameObject teammate;
    private Player player;
    private GameObject goal;
    private Rigidbody2D rb2d;

    
    private const float ballOffsetMultiplier = 0.98f;

    public bool IsCarryingBall { get; private set; } = false;

    private void Start()
    {
        player = this.EnsureComponent<Player>();
        playerMovement = this.EnsureComponent<PlayerMovement>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        rb2d = GetComponent<Rigidbody2D>();
        Ball = FindObjectOfType<Ball>().ThrowIfNull("Could not find ball");
        if (playerMovement != null && stateManager != null)
        {
            PlayerMovement actualPlayerMovement = playerMovement as PlayerMovement;
            if (actualPlayerMovement != null)
            {
                ballTurnSpeed = actualPlayerMovement.rotationSpeed / 250;
            }
        }
        laserGuide = this.GetComponent<LaserGuide>();

        NotificationManager notificationManager = GameManager.instance.notificationManager;
        notificationManager.CallOnMessage(Message.GoalScored, HandleGoalScored);
        stateManager.OnStateChange += HandleNewPlayerState;
    }

    private void HandleNewPlayerState(State oldState, State newState)
    {
        if (newState == State.Possession)
        {
            StartCarryingBall();
        }
        // TODO dkonik: Finish this up, got distracted
        stateManager.AttemptPossession(() => StartCarryingBall(), DropBall);
    }

        private void BlowBackEnemyPlayers()
    {
        if (player.Team == null)
        {
            return;
        }
        TeamManager enemyTeam = GameManager.instance.teams.Find((teamManager) => teamManager != player.Team);
        Debug.Assert(enemyTeam != null);

        {
            // Because C# doesn't have lvalue references. FML.
            GameObject effect = Instantiate(blowbackEffectPrefab, transform.position, transform.rotation);
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;

            col.enabled = true;

            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(player.Team.teamColor, 0.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f,  0.0f),
                    new GradientAlphaKey(0.25f, 0.75f),
                    new GradientAlphaKey(0.0f,  1.0f)
                }
            );
            col.color = grad;

            Destroy(effect, 1.0f);
        }

        foreach (Player enemyPlayer in enemyTeam.teamMembers)
        {
            Vector3 blowBackVector = enemyPlayer.transform.position - transform.position;
            if (blowBackVector.magnitude < blowbackRadius)
            {
                PlayerStun otherStun = enemyPlayer.GetComponent<PlayerStun>();
                PlayerStateManager otherStateManager = enemyPlayer.GetComponent<PlayerStateManager>();
                if (otherStun != null && otherStateManager != null)
                {
                    otherStateManager.AttemptStun(() => otherStun.StartStun(blowBackVector.normalized * blowbackForce, blowbackStunTime), otherStun.StopStunned);
                }
            }
        }
    }

    // This function is called when the BallCarrier initially gains possession
    // of the ball
    public void StartCarryingBall()
    {
        // TODO dkonik: Commented this out to make it compile. Fix this

        //BlowBackEnemyPlayers();
        //timeCarryStarted = Time.time;
        //ball.rigidbody.velocity = Vector2.zero;
        //ball.rigidbody.angularVelocity = 0;
        //CalculateOffset(ball);
        //if (slowMoOnCarry)
        //{
        //    GameManager.instance.SlowMo();
        //}
        //laserGuide?.DrawLaser();
        //carryBallCoroutine = StartCoroutine(CarryBall(ball));
    }

    private void CalculateOffset(Ball ball)
    {
        float? ballRadius = ball.GetComponent<CircleCollider2D>()?.bounds.extents.x;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null && ballRadius != null)
        {
            float spriteExtents = renderer.sprite.bounds.extents.x * transform.localScale.x;
            ballOffsetFromCenter = ballOffsetMultiplier * (spriteExtents + ballRadius.Value);
        }
    }



    private IEnumerator CarryBall()
    {
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        IsCarryingBall = true;
        //ball.Owner = this;

        while (true)
        {
            PlaceBallAtNose();
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator CoolDownTimer()
    {
        isCoolingDown = true;
        yield return new WaitForSeconds(coolDownTime);
        isCoolingDown = false;
    }

    public void DropBall()
    {
        if (Ball != null)
        {
            GameManager.instance.ResetSlowMo();
            StopCoroutine(carryBallCoroutine);
            carryBallCoroutine = null;

            // Reset references
            Ball.Owner = null;

            laserGuide?.StopDrawingLaser();
            if (this.isActiveAndEnabled)
            {
                StartCoroutine(CoolDownTimer());
            }
        }
        IsCarryingBall = false;
    }

    private Vector2 NosePosition(Ball ball)
    {
        Vector3 newPosition = transform.position + transform.right * ballOffsetFromCenter;
        return newPosition;
    }

    private void PlaceBallAtNose()
    {
        if (Ball != null)
        {
            Rigidbody2D rigidbody = Ball.GetComponent<Rigidbody2D>();
            Vector2 newPosition =
                CircularLerp(Ball.transform.position, NosePosition(Ball), transform.position,
                             ballOffsetFromCenter, Time.deltaTime, ballTurnSpeed);
            rigidbody.MovePosition(newPosition);
        }
    }

    private Vector2 CircularLerp(Vector2 start, Vector2 end, Vector2 center, float radius,
                         float timeDelta, float speed)
    {
        float angleMax = timeDelta * speed;
        Vector2 centeredStart = start - center;
        Vector2 centerToStartDirection = centeredStart.normalized;

        Vector2 centeredEndDirection = (end - center).normalized;
        float angle = Vector2.SignedAngle(centerToStartDirection, centeredEndDirection);
        float percentArc = Mathf.Clamp(angleMax / Mathf.Abs(angle / 360), 0, 1);

        Quaternion rotation = Quaternion.AngleAxis(angle * percentArc, Vector3.forward);
        Vector3 centeredResult = rotation * centerToStartDirection;
        centeredResult *= radius;
        return (Vector2)centeredResult + center;
    }

    #region EventHandlers
    private void HandleGoalScored()
    {
        // When a goal is scored, we want to let go of the ball
        if (IsCarryingBall)
        {
            stateManager.TransitionToState(State.NormalMovement);
        }
    }

    private void HandleCollision(GameObject thing)
    {
        Ball ball = thing.GetComponent<Ball>();
        if (ball == null || ball.Owner != null || !ball.Ownable || isCoolingDown || ball.Owner == this)
        {
            return;
        }

        if (stateManager.CurrentState == State.NormalMovement ||
            stateManager.CurrentState == State.Dash ||
            stateManager.CurrentState == State.ChargeDash ||
            stateManager.CurrentState == State.LayTronWall)
        {
            stateManager.TransitionToState(State.Possession);
        }
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    public void OnCollisionStay2D(Collision2D collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (stateManager != null && stateManager.IsInState(OldState.Dash))
        {
            HandleCollision(other.gameObject);
        }
    }

    private void OnDestroy()
    {
        DropBall();
    }
    #endregion
}
