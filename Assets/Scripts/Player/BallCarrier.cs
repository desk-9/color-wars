using System.Collections;
using UnityEngine;
using UtilityExtensions;

using EM = EventsManager;
// TODO: make PlayerMovemnt freeze on possession, unfreeze when ball unpossessed
// TODO: register callbacks in laserGuide
public class BallCarrier : MonoBehaviour
{
    // tweakables
    public float coolDownTime = .1f;
    public float timeCarryStarted {get; private set;}
    private float ballTurnSpeed; // = 10f;
    private float ballOffsetFromCenter = .5f;
    private const float ballOffsetMultiplier = 0.98f;
    // ball-charging tweakables
    public AnimationCurve chargeShotCurve;
    private float baseShotSpeed = 1.0f;
    private float maxShotSpeed = 56.0f;
    private float maxChargeShotTime = 1.5f;
    private float forcedShotTime = 3.75f;
    // Player null zone tweakables
    public const float playerNullZoneRadius = 0.1f;


    // references to other components
    public Ball ownedBall {get; private set;} = null;
    public TeamManager team;

    // TODO: figure out where to put this
    private ShotChargeIndicator shotChargeIndicator;

    // private data
    private Rigidbody2D ballRigidbody;
    private bool allowedToCarryBall = true;
    private bool isCarryingBall = false;
    private bool isChargingBall = false;
    public float shotSpeed {get; private set;} = 0.0f;

    // coroutines
    private Coroutine keepBallAtNose;
    private Coroutine forcedShotTimer;
    private Coroutine chargeShot;

    private void Start()
    {
        var player = GetComponent<Player>();
        team = player.team;
        var playerMovement = GetComponent<PlayerMovement>();
        ballTurnSpeed = playerMovement.rotationSpeed / 250;
    }

    public bool IsInNullZone()
    {
        Collider2D collider = Physics2D.OverlapCircle(
            transform.position, playerNullZoneRadius, LayerMask.GetMask("NullZone"));
        return collider != null;
    }

    public bool CanCarry(Ball ball)
    {
        return ball.CanBeCarriedBy(this) && this.allowedToCarryBall;
    }
    public void Carry(Ball ball)
    {
        ball.SetOwner(this);
        StartCarrying(ball);
    }

    public bool CanSteal(Ball ball)
    {
        return ball.CanBeStolenBy(this) && this.allowedToCarryBall;
    }
    public void Steal(Ball ball)
    {
        // Do the "steal"
        BallCarrier oldOwner = ball.GetOwner();
        oldOwner.StopCarrying();
        Carry(ball);

        // Fire onBallStolen event
        EM.RaiseOnBallStolen(
            new EM.onBallStolenArgs
            {
                oldOwner = oldOwner,
                newOwner = this,
                ball = ball
            });
    }

    public void Drop()
    {
        ownedBall.SetOwner(null);
        SetShootVelocity(ownedBall);
        Ball ball = StopCarrying();
        EM.RaiseOnBallDropped(
            new EM.onBallDroppedArgs{ballCarrier = this, ball = ball});
    }

    public void Shoot()
    {
        ownedBall.SetOwner(null);
        SetShootVelocity(ownedBall);
        Ball ball = StopCarrying();
        EM.RaiseOnBallShot(
            new EM.onBallShotArgs{ballCarrier = this, ball = ball});
    }
    private void SetShootVelocity(Ball ball)
    {
        Vector3 shotDirection = ball.transform.position - transform.position;
        ballRigidbody.velocity = shotDirection.normalized * shotSpeed;
    }

    private IEnumerator ForcedShotTimer()
    {
        float elapsedTime = 0.0f;
        while (elapsedTime < forcedShotTime)
        {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        if (!isChargingBall)
        {
            Drop();
        }
        else
        {
            Shoot();
        }
    }

    private void StartCarrying(Ball ball)
    {
        // Initialize data members -- very first thing to do!!!
        this.timeCarryStarted = Time.time;
        this.ownedBall = ball;
        this.ballRigidbody = ball.GetComponent<Rigidbody2D>();
        this.isCarryingBall = true;
        this.isChargingBall = false;
        this.shotSpeed = baseShotSpeed;

        // Initialize coroutines
        keepBallAtNose = this.StartCoroutine(KeepBallAtNose());
        forcedShotTimer = this.StartCoroutine(ForcedShotTimer());
        chargeShot = CoroutineUtility.ForceStopCoroutine(chargeShot);

        // Fire onStartedCarryingBall
        EM.RaiseOnStartedCarryingBall(
            new EM.onStartedCarryingBallArgs{ballCarrier = this, ball = ball});
    }

    private Ball StopCarrying()
    {
        Ball ball = this.ownedBall; // save for return value

        // Clean up coroutines
        keepBallAtNose = CoroutineUtility.ForceStopCoroutine(keepBallAtNose);
        forcedShotTimer = CoroutineUtility.ForceStopCoroutine(forcedShotTimer);

        // Deal with shot charging
        if (this.isChargingBall)
        {
            StopCharging(ball);
        }

        // Fire onStoppedCarryingBall
        EM.RaiseOnStoppedCarryingBall(
            new EM.onStoppedCarryingBallArgs{ballCarrier = this, ball = ball});

        // Clean up data members -- very last thing to do!!!
        this.ownedBall = null;
        this.ballRigidbody = null;
        this.isCarryingBall = false;
        // NOTE: this should already be set by StopCharging(). but doesn't hurt
        // to set it here also (makes the code more symmetric with
        // StartCarrying)
        this.isChargingBall = false;
        this.shotSpeed = baseShotSpeed;

        // Return ball (allows caller to fire Event with a valid reference to
        // the ball)
        return ball;
    }

    // NOTE: This cannot be called until *after* StartCarrying has run
    // Should probably be called with OnShootPressed
    public void StartCharging()
    {
        if (!isCarryingBall)
        {
            Debug.LogWarning("Warning: StartCharging was called but !isCarryingBall!");
            return;
        }
        if (isChargingBall)
        {
            return;
        }
        isChargingBall = true;
        chargeShot = StartCoroutine(
            TransitionUtility.LerpFloat(
                floatSetter: this.SetShotSpeed,
                startValue: baseShotSpeed,
                endValue: maxShotSpeed,
                duration: maxChargeShotTime,
                useGameTime: true,
                animationCurve: chargeShotCurve));

        // TODO: figure out where to put this
        shotChargeIndicator.Show();

        EM.RaiseOnStartedChargingBall(
            new EM.onStartedChargingBallArgs{ballCarrier = this, ball = ownedBall});
    }
    private void SetShotSpeed(float value)
    {
        this.shotSpeed = value;
        shotChargeIndicator.FillAmount = value;
    }

    // NOTE: This should only be called from StopCarrying
    // Should probably be called with OnShootReleased
    private void StopCharging(Ball ball)
    {
        if (!isCarryingBall)
        {
            Debug.LogWarning("Warning: StopCharging was called but !isCarryingBall!");
            return;
        }
        if (!isChargingBall)
        {
            return;
        }
        if (chargeShot != null)
        {
            EM.RaiseOnStoppedChargingBall(
                new EM.onStoppedChargingBallArgs{ballCarrier = this, ball = ball});
            this.StopCoroutine(chargeShot);
            chargeShot = null;
        }
        isChargingBall = false;
    }


    // +----------------------------------+
    // | COROUTINES                       |
    // +----------------------------------+
    private IEnumerator CoolDownTimer()
    {
        allowedToCarryBall = false;
        yield return new WaitForSeconds(coolDownTime);
        allowedToCarryBall = true;
    }

    private IEnumerator KeepBallAtNose()
    {
        while (true)
        {
            PlaceBallAtNose();
            yield return new WaitForFixedUpdate();
        }
    }
    private void PlaceBallAtNose()
    {
        Debug.Assert(ownedBall != null);
        Vector2 nosePosition = (transform.position
                                + transform.right * ballOffsetFromCenter);
        Vector2 newPosition = CircularLerp(
            ownedBall.transform.position, nosePosition,
            transform.position, ballOffsetFromCenter,
            Time.deltaTime, ballTurnSpeed);
        ballRigidbody.MovePosition(newPosition);
    }
    private Vector2 CircularLerp(Vector2 start, Vector2 end,
                                 Vector2 center, float radius,
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

    private void HandleCollision(GameObject thing)
    {
        Ball ball = thing.GetComponent<Ball>();
        if (ball == null)
        {
            return;
        }
        if (this.CanCarry(ball))
        {
            this.Carry(ball);
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
        HandleCollision(other.gameObject);
    }

    private void OnDestroy()
    {
        StopCarrying();
    }

}
