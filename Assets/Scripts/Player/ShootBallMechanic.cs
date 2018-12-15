using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class ShootBallMechanic : MonoBehaviour
{
    // These control how the charging progresses
    public AnimationCurve chargeShotCurve;
    public float baseShotSpeed = 1.0f;
    public float maxShotSpeed = 56.0f; // this value is from the previous charge-shot logic
    // REMARK: An assumption is made in Start() that maxChargeShotTime <=
    // forcedShotTime. Update that logic if this assumption is broken
    public float maxChargeShotTime;
    public float forcedShotTime;
    public float shotSpeed { get; private set; } = 1.0f;

    private float elapsedTime = 0.0f;
    private CircularTimer circularTimer;
    private Coroutine shootTimerCoroutine;
    private ShotChargeIndicator shotChargeIndicator;
    private Coroutine chargeShotCoroutine;
    private PlayerMovement playerMovement;
    private PlayerStateManager stateManager;
    private BallCarrier ballCarrier;
    private Player teamMate;
    private Player player;

    private void Start()
    {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        ballCarrier = this.EnsureComponent<BallCarrier>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        player = this.EnsureComponent<Player>();

        GameManager.instance.NotificationManager.CallOnMessageIfSameObject(
            Message.PlayerPressedShoot, OnShootPressed, gameObject);
        GameManager.instance.NotificationManager.CallOnMessageIfSameObject(
            Message.PlayerReleasedShoot, OnShootReleased, gameObject);
        stateManager.OnStateChange += HandleNewPlayerState;

        Ball ball = GameObject.FindObjectOfType<Ball>();

        InitializeCircularIndicators(); // This is for team selection screen

        // In this situation, maxChargeShotTime is irrelevant => change it to
        // make later logic more elegant. NOTE: If removing this, also change
        // the comment above (by the declaration of maxChargeShotTime)
        if (maxChargeShotTime <= 0.0f)
        {
            maxChargeShotTime = forcedShotTime;
        }
    }

    private void HandleNewPlayerState(State oldState, State newState)
    {
        if (newState == State.Possession)
        {
            StartPossessionTimer();
        } else if (newState == State.ChargeShot)
        {
            StartChargeShot();
        }

        if ((oldState == State.Possession && newState != State.ChargeShot) ||
            (oldState == State.ChargeShot))
        {
            StopShootBallCoroutines();
        }
    }

    private void StartChargeShot()
    {
        shotChargeIndicator.Show();
        chargeShotCoroutine = StartCoroutine(TransitionUtility.LerpFloat((value) =>
            {
                this.shotSpeed = value;
                shotChargeIndicator.FillAmount = value;
            },
            startValue: baseShotSpeed, endValue: maxShotSpeed,
            duration: maxChargeShotTime,
            useGameTime: true, animationCurve: chargeShotCurve)
            );
    }

    // Initialize the circular timer (forced shot timeout) and the
    // ShotChargeIndicator -- these are the little circular dials that show
    // up when a player possesses the ball, and when a player charges their
    // shot (respectively).
    private void InitializeCircularIndicators(TeamManager team = null)
    {
        // Need to destroy preexisting objects (e.g. if selecting teams, and
        // then switching team)
        if (shotChargeIndicator != null)
        {
            Destroy(shotChargeIndicator);
        }
        if (circularTimer != null)
        {
            Destroy(circularTimer);
        }

        // Circular timer
        GameObject circularTimerPrefab = GameManager.instance.neutralResources.circularTimerPrefab;
        circularTimer = Instantiate(
            circularTimerPrefab, transform).GetComponent<CircularTimer>();

        // ShotCharge indicator
        GameObject shotChargeIndicatorPrefab = GameManager.instance.neutralResources.shotChargeIndicatorPrefab;
        shotChargeIndicator = Instantiate(
            shotChargeIndicatorPrefab, transform).GetComponent<ShotChargeIndicator>();

        // REMARK: See comment below ("[Krista fri 4/13] I found this little
        // gem...") for an explanation of why we need to set minFillAmount and
        // maxFillAmount like this
        shotChargeIndicator.minFillAmount = baseShotSpeed;
        shotChargeIndicator.maxFillAmount = maxShotSpeed;
    }

    private void StartPossessionTimer()
    {
        bool shootTimerRunning = shootTimerCoroutine != null;
        bool alreadyChargingShot = chargeShotCoroutine != null;
        if (!shootTimerRunning || !alreadyChargingShot || stateManager.CurrentState == State.Possession)
        {
            shootTimerCoroutine = StartCoroutine(ShootTimer());
        }
    }

    private IEnumerator ShootTimer()
    {
        circularTimer?.StartTimer(forcedShotTime, delegate { });
        shotSpeed = baseShotSpeed;
        elapsedTime = 0.0f;
        while (elapsedTime < forcedShotTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Shoot();
    }

    // Warning: this function may be called any time the player presses the A
    // button (or whatever xbox controller button is bound to shoot). This
    // includes dash
    private void OnShootPressed()
    {
        bool shootTimerRunning = shootTimerCoroutine != null;
        bool alreadyChargingShot = chargeShotCoroutine != null;
        if (stateManager.CurrentState == State.Possession
            && shootTimerRunning && !alreadyChargingShot)
        {
            stateManager.TransitionToState(State.ChargeShot);
        }
    }

    private void OnShootReleased()
    {
        bool shootTimerRunning = shootTimerCoroutine != null;
        bool alreadyChargingShot = chargeShotCoroutine != null;
        if (stateManager.CurrentState == State.ChargeShot && alreadyChargingShot)
        {
            Debug.Assert(shootTimerRunning == true);
            Shoot();
        }
    }

    private void Shoot()
    {
        // Set the information
        Ball ball = ballCarrier.Ball.ThrowIfNull("Tried to shoot ball while [ballCarrier.Ball] is null");
        NormalMovementInformation shootBallInfo = stateManager.GetStateInformationForWriting<NormalMovementInformation>(State.NormalMovement);
        shootBallInfo.ShotBall = true;
        shootBallInfo.BallStartPosition = ball.CurrentPosition;
        shootBallInfo.Direction = ball.CurrentPosition - (Vector2)transform.position;
        shootBallInfo.Strength = shotSpeed;

        // Cleanup and transition states
        StopShootBallCoroutines();
        stateManager.TransitionToState(State.NormalMovement, shootBallInfo);
    }

    private void StopShootBallCoroutines()
    {
        if (shootTimerCoroutine != null)
        {
            circularTimer?.StopTimer();
            StopCoroutine(shootTimerCoroutine);
            shootTimerCoroutine = null;
        }
        if (chargeShotCoroutine != null)
        {
            shotChargeIndicator.Stop();
            StopCoroutine(chargeShotCoroutine);
            chargeShotCoroutine = null;
        }
    }

}
