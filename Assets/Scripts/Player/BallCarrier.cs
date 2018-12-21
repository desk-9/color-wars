using System.Collections;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using UtilityExtensions;

public class BallCarrier : MonoBehaviour
{
    public GameObject blowbackEffectPrefab;
    public float coolDownTime = .1f;
    public Ball Ball { private set; get; }

    private float ballTurnSpeed = 10f;
    public bool slowMoOnCarry = true;
    public float aimAssistThreshold = 20f;
    public float aimAssistLerpAmount = .5f;
    public float goalAimAssistOffset = 1f;
    public float delayBetweenSnaps = .2f;
    public float snapEpsilon = 5f;
    public float snapLerpStrength = .5f;
    public float timeCarryStarted { get; private set; }
    public float blowbackRadius = 3f;
    public float blowbackForce = 5f;
    public float blowbackStunTime = 0.1f;
    private float ballOffsetFromCenter = .5f;
    private IPlayerMovement playerMovement;
    private PlayerStateManager stateManager;
    private Coroutine carryBallCoroutine;
    private bool isCoolingDown = false;
    private LaserGuide laserGuide;
    private GameObject teammate;
    private Player player;
    private GameObject goal;
    private Rigidbody2D rb2d;
    private GameObject snapToObject;
    private float snapDelay = 0f;
    private Vector2 stickAngleWhenSnapped;
    private const float ballOffsetMultiplier = 0.98f;

    public bool IsCarryingBall { get; private set; } = false;

    private void Start()
    {
        snapToObject = null;
        player = GetComponent<Player>();
        playerMovement = GetComponent<IPlayerMovement>();
        stateManager = GetComponent<PlayerStateManager>();
        rb2d = GetComponent<Rigidbody2D>();
        if (playerMovement != null && stateManager != null)
        {
            stateManager.CallOnStateEnter(
                State.Posession, playerMovement.FreezePlayer);
            stateManager.CallOnStateExit(
                State.Posession, playerMovement.UnFreezePlayer);
            PlayerMovement actualPlayerMovement = playerMovement as PlayerMovement;
            if (actualPlayerMovement != null)
            {
                ballTurnSpeed = actualPlayerMovement.rotationSpeed / 250;
            }
        }
        laserGuide = this.GetComponent<LaserGuide>();
        this.FrameDelayCall(() => { GetGoal(); GetTeammate(); }, 2);

        NotificationManager notificationManager = GameManager.instance.notificationManager;
        notificationManager.CallOnMessage(Message.GoalScored, HandleGoalScored);
        notificationManager.CallOnMessageWithPlayerObject(
            Message.BallPossessedByPlayer,
            (player) => {
                if (IsLocalPlayer() && Ball == null) {
                    stateManager.AttemptPossession(() => StartCarryingBall(GameManager.instance.ball), DropBall);
                }
            });
    }

    private void BlowBackEnemyPlayers()
    {
        if (player.team == null)
        {
            return;
        }
        TeamManager enemyTeam = GameManager.instance.teams.Find((teamManager) => teamManager != player.team);
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
                    new GradientColorKey(player.team.teamColor, 0.0f)
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

    bool IsLocalPlayer() {
        var playerView = GetComponent<PhotonView>();
        return playerView && playerView.IsMine;
    }

    // This function is called when the BallCarrier initially gains possession
    // of the ball
    public void StartCarryingBall(Ball ball)
    {

        var ballView = ball.GetComponent<PhotonView>();
        if (IsLocalPlayer()) {
            ballView.TransferOwnership(PhotonNetwork.LocalPlayer);
        }
        BlowBackEnemyPlayers();
        timeCarryStarted = Time.time;
        ball.rigidbody.velocity = Vector2.zero;
        ball.rigidbody.angularVelocity = 0;
        CalculateOffset(ball);
        if (slowMoOnCarry)
        {
            GameManager.instance.SlowMo();
        }
        laserGuide?.DrawLaser();
        carryBallCoroutine = StartCoroutine(CarryBall(ball));
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

    private void GetTeammate()
    {
        TeamManager team = player.team;
        if (team == null)
        {
            return;
        }
        foreach (Player teammate in team.teamMembers)
        {
            if (teammate != player)
            {
                this.teammate = teammate.gameObject;
            }
        }
    }

    private void GetGoal()
    {
        goal = GameObject.FindObjectOfType<GoalAimPoint>()?.gameObject;
    }

    private void SnapToGameObject()
    {
        Vector3 vector = (snapToObject.transform.position - transform.position).normalized;
        rb2d.rotation = Vector2.SignedAngle(Vector2.right, Vector2.Lerp(transform.right, vector, snapLerpStrength));
    }

    private void SnapAimTowardsTargets()
    {
        if (playerMovement == null
            || player == null)
        {
            return;
        }
        if (snapDelay > 0f)
        {// || TutorialLiveClips.runningLiveClips || PlayerRecorder.isRecording) {
            playerMovement?.RotatePlayer();
            return;
        }
        PlayerMovement pm = (PlayerMovement)playerMovement;
        Vector2 stickDirection = pm.lastDirection;
        if (snapToObject != null)
        {
            Vector3 vector = (snapToObject.transform.position - transform.position).normalized;
            if (stickDirection == Vector2.zero ||
                Mathf.Abs(Vector2.Angle(vector, stickDirection)) < aimAssistThreshold ||
                Mathf.Abs(Vector2.Angle(stickAngleWhenSnapped, stickDirection)) < snapEpsilon)
            {
                SnapToGameObject();
            }
            else
            {
                snapDelay = delayBetweenSnaps;
                snapToObject = null;
                playerMovement?.RotatePlayer();
            }
        }
        else
        {
            if (stickDirection == Vector2.zero)
            {
                playerMovement?.RotatePlayer();
                return;
            }

            Vector2? goalVector = null;
            Vector2? teammateVector = null;
            if (goal != null)
            {
                goalVector = ((goal.transform.position + Vector3.up) - transform.position).normalized;
            }
            if (teammate != null)
            {
                teammateVector = (teammate.transform.position - transform.position).normalized;
            }

            if (goalVector.HasValue &&
                    Mathf.Abs(Vector2.Angle(transform.right, goalVector.Value)) < aimAssistThreshold &&
                Ball.renderer.color == player.team.teamColor.color)
            {
                snapToObject = goal;
                stickAngleWhenSnapped = stickDirection;
                SnapToGameObject();
            }
            else if (teammateVector.HasValue &&
                         Mathf.Abs(Vector2.Angle(transform.right, teammateVector.Value)) < aimAssistThreshold)
            {
                snapToObject = teammate;
                stickAngleWhenSnapped = stickDirection;
                SnapToGameObject();
            }
            else
            {
                playerMovement?.RotatePlayer();
            }
        }
    }

    private IEnumerator CarryBall(Ball ball)
    {
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        IsCarryingBall = true;
        this.Ball = ball;
        ball.Owner = this;
        snapToObject = null;

        while (true)
        {
            SnapAimTowardsTargets();
            PlaceBallAtNose();
            snapDelay -= Time.fixedDeltaTime;
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
            snapToObject = null;

            // Reset references
            Ball.Owner = null;
            Ball = null;

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
            stateManager?.CurrentStateHasFinished();
        }
    }

    private void HandleCollision(GameObject thing)
    {
        Ball ball = thing.GetComponent<Ball>();
        if (ball == null || ball.Owner != null || !ball.Ownable || isCoolingDown)
        {
            return;
        }
        if (stateManager != null)
        {
            TeamManager last_team = ball.LastOwner?.GetComponent<Player>().team;
            TeamManager this_team = GetComponent<Player>().team;
            stateManager.AttemptPossession(() => StartCarryingBall(ball), DropBall);
        }
        else
        {
            StartCoroutine(CoroutineUtility.RunThenCallback(CarryBall(ball), DropBall));
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
        if (stateManager != null && stateManager.IsInState(State.Dash))
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
