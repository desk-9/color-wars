using System.Collections;
using UnityEngine;
using UtilityExtensions;
using Photon.Pun;
using Photon.Realtime;

public class BallCarrier : MonoBehaviour
{
    [SerializeField]
	private float blowbackRadius = 15f;
    [SerializeField]
	private float blowbackForce = 100f;
    [SerializeField]
	private float blowbackStunTime = 0.2f;

    public GameObject blowbackEffectPrefab;
    public float coolDownTime = .1f;
    public Ball Ball { private set; get; }

    private float ballTurnSpeed = 10f;
    public bool slowMoOnCarry = true;

    public float timeCarryStarted { get; private set; }
    private float ballOffsetFromCenter;
    private PlayerMovement playerMovement;
    private PlayerStateManager stateManager;
    private Coroutine carryBallCoroutine;
    private bool isCoolingDown = false;
    private LaserGuide laserGuide;
    private GameObject teammate;
    private Player player;
    private GameObject goal;
    private PhotonView photonView;


    private const float ballOffsetMultiplier = 0.98f;

    public bool IsCarryingBall { get; private set; } = false;

    private void Start()
    {
        player = this.EnsureComponent<Player>();
        playerMovement = this.EnsureComponent<PlayerMovement>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        Ball = FindObjectOfType<Ball>().ThrowIfNull("Could not find ball");
        photonView = this.EnsureComponent<PhotonView>();
        if (playerMovement != null && stateManager != null)
        {
            PlayerMovement actualPlayerMovement = playerMovement as PlayerMovement;
            if (actualPlayerMovement != null)
            {
                ballTurnSpeed = actualPlayerMovement.rotationSpeed / 250;
            }
        }
        laserGuide = this.GetComponent<LaserGuide>();

        NotificationManager notificationManager = GameManager.NotificationManager;
        notificationManager.CallOnMessage(Message.GoalScored, HandleGoalScored);
        stateManager.OnStateChange += HandleNewPlayerState;
        CalculateOffset();
    }

    private void HandleNewPlayerState(State oldState, State newState)
    {
        if (newState == State.Possession)
        {
            StartCarryingBall();
        }

        if ((oldState == State.Possession && newState != State.ChargeShot) ||
            oldState == State.ChargeShot)
        {
            DropBall();
        }
    }

    /// <summary>
    /// Stuns the players that should get blown back
    /// </summary>
    private void StunNearbyPlayers()
    {
        // Stun the players that should get blown back
        TeamManager enemyTeam = GameManager.Instance.Teams.Find((teamManager) => teamManager != player.Team);
        Debug.Assert(enemyTeam != null);

        foreach (Player enemyPlayer in enemyTeam.teamMembers)
        {
            Vector3 blowBackVector = enemyPlayer.transform.position - transform.position;

            if (blowBackVector.magnitude < blowbackRadius &&
                enemyPlayer.StateManager.IsInState(State.NormalMovement, State.LayTronWall, State.Dash))
            {
                enemyPlayer.StateManager.StunNetworked(
                    enemyPlayer.PlayerMovement.CurrentPosition,
                    blowBackVector.normalized * blowbackForce,
                    blowbackStunTime,
                    false
                    );
            }
        }
    }


    private void DoBlowbackEffect()
    {
        TeamManager enemyTeam = GameManager.Instance.Teams.Find((teamManager) => teamManager != player.Team);
        Debug.Assert(enemyTeam != null);

        {
            // TODO dkonik: Don't instantiate this every time, reuse
            // Because C# doesn't have lvalue references. FML.
            GameObject effect = Instantiate(blowbackEffectPrefab, transform.position, transform.rotation);
            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;

            col.enabled = true;

            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(player.Team.TeamColor, 0.0f)
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
    }

    // This function is called when the BallCarrier initially gains possession
    // of the ball
    public void StartCarryingBall()
    {
        DoBlowbackEffect();
        timeCarryStarted = Time.time;

        // TODO dkonik: Make the laser guide event based
        laserGuide?.DrawLaser();
        carryBallCoroutine = StartCoroutine(CarryBall());
    }

    private void CalculateOffset()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            float spriteExtents = renderer.sprite.bounds.extents.x * transform.localScale.x;
            ballOffsetFromCenter = ballOffsetMultiplier * (spriteExtents + Ball.Radius);
        }
    }

    private IEnumerator CarryBall()
    {
        IsCarryingBall = true;
        if (photonView.OwnerActorNr == PhotonNetwork.LocalPlayer.ActorNumber) {
            Ball.TakeOwnership();
        }
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

    private void DropBall()
    {
        if (Ball != null)
        {
            if (carryBallCoroutine != null)
            {
                StopCoroutine(carryBallCoroutine);
                carryBallCoroutine = null;
            }

            // TODO dkonik: This should be event based
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
        Vector2 newPosition = playerMovement.CurrentPosition + playerMovement.Forward * ballOffsetFromCenter;
        return newPosition;
    }

    private void PlaceBallAtNose()
    {
        if (Ball != null)
        {
            Vector2 newPosition =
                CircularLerp(Ball.transform.position, NosePosition(Ball), transform.position,
                             ballOffsetFromCenter, Time.deltaTime, ballTurnSpeed);
            Ball.MoveTo(newPosition);
        } else
        {
            Debug.LogError("Ball is null in BallCarrier. Should not ever happen");
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
        if (ball == null || ball.Owner != null || !ball.Ownable || isCoolingDown || ball.Owner == player)
        {
            return;
        }

        if (stateManager.CurrentState == State.NormalMovement ||
            stateManager.CurrentState == State.Dash ||
            stateManager.CurrentState == State.ChargeDash ||
            stateManager.CurrentState == State.LayTronWall)
        {
            StunNearbyPlayers();

            PossessBallInformation info = stateManager.GetStateInformationForWriting<PossessBallInformation>(State.Possession);
            info.StoleBall = false;
            stateManager.TransitionToState(State.Possession, info);
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
        if (stateManager != null && stateManager.CurrentState == State.Dash)
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
