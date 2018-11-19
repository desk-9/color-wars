using System;
using System.Collections;
using UnityEngine;
using UtilityExtensions;


public class PlayerMovement : MonoBehaviour, IPlayerMovement
{

    public float movementSpeed;
    public float rotationSpeed = 1080;
    public bool freezeRotation = false;
    public float maxAwayFromBallAngle = 10f;

    public bool instantRotation { get; set; } = true;

    private Rigidbody2D rb2d;
    private Coroutine playerMovementCoroutine = null;
    private PlayerStateManager stateManager;

    public Vector2 lastDirection = Vector2.zero;

    public const float minBallForceRotationTime = 0.1f;

    private void StartPlayerMovement()
    {
        if (playerMovementCoroutine != null)
        {
            StopCoroutine(playerMovementCoroutine);
        }

        playerMovementCoroutine = StartCoroutine(Move());
    }

    private IEnumerator RotateOnly()
    {
        while (true)
        {
            RotatePlayer();
            yield return null;
        }
    }

    public void StartRotateOnly()
    {
        if (playerMovementCoroutine != null)
        {
            StopCoroutine(playerMovementCoroutine);
        }

        playerMovementCoroutine = StartCoroutine(RotateOnly());
    }

    public void StopAllMovement()
    {
        if (playerMovementCoroutine != null)
        {
            StopCoroutine(playerMovementCoroutine);
            rb2d.velocity = Vector2.zero;
        }
    }

    public void FreezePlayer()
    {
        rb2d.isKinematic = true;
    }

    public void UnFreezePlayer()
    {
        rb2d.isKinematic = false;
    }

    public void RotatePlayer()
    {
        if (freezeRotation)
        {
            return;
        }
        Vector2 direction = lastDirection;
        if (direction != Vector2.zero)
        {
            // Only do if nonzero, otherwise [SignedAngle] returns 90 degrees
            // and player snaps to up direction
            if (instantRotation)
            {
                rb2d.rotation = Vector2.SignedAngle(Vector2.right, direction);
            }
            else
            {
                float maxAngleChange = Vector2.SignedAngle(transform.right, direction);
                float sign = Mathf.Sign(maxAngleChange);
                float speedChange = rotationSpeed * Time.deltaTime;
                float actualChange = sign * Mathf.Min(Mathf.Abs(maxAngleChange), speedChange);
                float finalRotation = rb2d.rotation + actualChange;
                if (finalRotation <= 0)
                {
                    finalRotation = 360 - Mathf.Repeat(-finalRotation, 360);
                }
                finalRotation = Mathf.Repeat(finalRotation, 360);
                BallCarrier ballCarrier = GetComponent<BallCarrier>();
                if (ballCarrier != null && ballCarrier.Ball != null
                    && (Time.time - ballCarrier.timeCarryStarted) >= minBallForceRotationTime)
                {
                    Ball ball = ballCarrier.Ball;
                    Vector3 ballDirection = (ball.transform.position - transform.position).normalized;
                    Vector3 unitFinal = Quaternion.AngleAxis(finalRotation, Vector3.forward) * Vector2.right;
                    float angleDifference = Vector2.SignedAngle(ballDirection, unitFinal);
                    if (Mathf.Abs(angleDifference) >= maxAwayFromBallAngle)
                    {
                        finalRotation =
                            Vector2.SignedAngle(Vector2.right, ballDirection)
                            + Mathf.Sign(angleDifference) * maxAwayFromBallAngle;
                    }
                }
                if (finalRotation <= 0)
                {
                    finalRotation = 360 - Mathf.Repeat(-finalRotation, 360);
                }
                finalRotation = Mathf.Repeat(finalRotation, 360);
                rb2d.rotation = finalRotation;
            }
        }
    }

    private IEnumerator Move()
    {
        float startTime = Time.time;
        yield return new WaitForFixedUpdate();

        while (true)
        {
            rb2d.velocity = movementSpeed * lastDirection;
            // TODO: TUTORIAL
            if (lastDirection.magnitude > 0.1f)
            {
                if (Time.time - startTime > 0.75f)
                {
                    GameManager.instance.notificationManager.NotifyStringEvent("MoveTutorial", this.gameObject);
                }
            }
            else
            {
                startTime = Time.time;
            }

            RotatePlayer();
            yield return null;
        }
    }

    // Use this for initialization
    private void Start()
    {
        rb2d = this.EnsureComponent<Rigidbody2D>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        GameManager.instance.notificationManager.CallOnMessageWithSender(
            Message.PlayerStick, playerPair =>
            {
                Tuple<Vector2, GameObject> pair = playerPair as Tuple<Vector2, GameObject>;
                GameObject player = pair?.Item2;
                if (pair != null && this != null && player == this.gameObject)
                {
                    lastDirection = pair.Item1;
                }
            });
        stateManager.AttemptNormalMovement(StartPlayerMovement, StopAllMovement);
    }
}
