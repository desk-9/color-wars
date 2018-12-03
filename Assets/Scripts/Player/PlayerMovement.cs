using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;


public class PlayerMovement : MonoBehaviour
{
    /// <summary>
    /// There are some states where we don't want other players to be able to push around the
    /// local player, so we make them kinematic for those states
    /// </summary>
    private HashSet<State> kinematicStates = new HashSet<State>
    {
        // TODO dkonik: It kind of seems like there are a few more states that would want this,
        // like charge ChargeShot, maybe actual Dash? But they didn't at the time, so I didn't add them.
        // However, if it makes sense, add them
        State.ChargeDash,
        State.Possession
    };

    /// <summary>
    /// States where the player is only able to rotate
    /// </summary>
    private HashSet<State> rotateOnlyStates = new HashSet<State>
    {
        State.ChargeDash,
        State.Possession,
        State.ChargeShot,
        State.StartOfMatch
    };

    /// <summary>
    /// States where the position/movement of the player is not controlled by the controller,
    /// but momentarily controlled by something else
    /// </summary>
    private HashSet<State> externalControlStates = new HashSet<State>
    {
        State.Dash,
        State.Stun,
        State.LayTronWall,
    };

    public float movementSpeed;
    public float rotationSpeed = 1080;
    public bool freezeRotation = false;
    public float maxAwayFromBallAngle = 10f;

    public bool instantRotation { get; set; } = true;

    private Rigidbody2D rb2d;
    private Coroutine playerMovementCoroutine = null;
    private PlayerStateManager stateManager;
    private float aimAssistCooldownRemaining = 0f;
    private GameObject aimAssistTarget;
    private Vector2 lastDirection = Vector2.zero;
    private Vector2 stickAngleWhenSnapped;
    private GameObject goal;
    private GameObject teammate;
    private Ball ball;
    private Player player;

    [SerializeField]
    private float aimAssistThreshold = 20f;
    [SerializeField]
    private float aimAssistLerpAmount = .5f;
    [SerializeField]
    private float goalAimAssistOffset = 1f;
    [SerializeField]
    private float delayBetweenSnaps = .2f;
    [SerializeField]
    private float aimAssistEpsilon = 3.5f;
    [SerializeField]
    private float aimAssistLerpStrength = .2f;

    public const float minBallForceRotationTime = 0.1f;

    private void StartNormalMovement()
    {
        if (playerMovementCoroutine != null)
        {
            StopCoroutine(playerMovementCoroutine);
        }

        playerMovementCoroutine = StartCoroutine(Move());
    }

    private IEnumerator RotateOnly(bool snapToGameObjects)
    {
        aimAssistTarget = null;
        while (true)
        {
            if (snapToGameObjects)
            {
                RotateWithAimAssist();
            } else
            {
                RotatePlayer();
            }
            yield return null;
        }
    }

    private void StartRotateOnly(bool snapToGameObjects)
    {
        if (playerMovementCoroutine != null)
        {
            StopCoroutine(playerMovementCoroutine);
        }

        playerMovementCoroutine = StartCoroutine(RotateOnly(snapToGameObjects));
    }

    private void StopAllMovement(bool zeroOutVelocity = false)
    {
        if (playerMovementCoroutine != null)
        {
            StopCoroutine(playerMovementCoroutine);

            if (zeroOutVelocity)
            {
                rb2d.velocity = Vector2.zero;
            }
        }
    }

    private void RotatePlayer()
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

    private void AimAssistTowardsTarget()
    {
        Vector3 vector = (aimAssistTarget.transform.position - transform.position).normalized;
        rb2d.rotation = Vector2.SignedAngle(Vector2.right, Vector2.Lerp(transform.right, vector, aimAssistLerpStrength));
    }

    /// <summary>
    /// Tries to snap to the goal or teammate if it can, otherwise just rotates
    /// </summary>
    private void RotateWithAimAssist()
    {
        // If we are cooling down still, dont snap
        if (aimAssistCooldownRemaining > 0f)
        {
            RotatePlayer();
            return;
        }

        if (aimAssistTarget != null)
        {
            Vector3 vector = (aimAssistTarget.transform.position - transform.position).normalized;
            if (lastDirection == Vector2.zero ||
                Mathf.Abs(Vector2.Angle(vector, lastDirection)) < aimAssistThreshold ||
                Mathf.Abs(Vector2.Angle(stickAngleWhenSnapped, lastDirection)) < aimAssistEpsilon)
            {
                AimAssistTowardsTarget();
            }
            else
            {
                aimAssistCooldownRemaining = delayBetweenSnaps;
                aimAssistTarget = null;
                RotatePlayer();
            }
        }
        else
        {
            if (lastDirection == Vector2.zero)
            {
                RotatePlayer();
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

            // TODO dkonik: Ugly to be directly checking the balls color like this
            if (goalVector.HasValue &&
                    Mathf.Abs(Vector2.Angle(transform.right, goalVector.Value)) < aimAssistThreshold &&
                ball.renderer.color == player.team.teamColor.color)
            {
                aimAssistTarget = goal;
                stickAngleWhenSnapped = lastDirection;
                AimAssistTowardsTarget();
            }
            else if (teammateVector.HasValue &&
                         Mathf.Abs(Vector2.Angle(transform.right, teammateVector.Value)) < aimAssistThreshold)
            {
                aimAssistTarget = teammate;
                stickAngleWhenSnapped = lastDirection;
                AimAssistTowardsTarget();
            }
            else
            {
                RotatePlayer();
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

            // TODO dkonik: Remove this code. This is for the tutorial.
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
        stateManager.OnStateChange += HandleNewPlayerState;

        // TODO dkonik: This may need to be pushed a frame back, to provide other components the
        // ability to subscribe to this...though I doubt anything is waiting on the
        // StartOfMatch state
        stateManager.TransitionToState(State.StartOfMatch);
        player = this.EnsureComponent<Player>();
        goal = GameObject.FindObjectOfType<GoalAimPoint>()?.gameObject;
        ball = GameObject.FindObjectOfType<Ball>();
        this.FrameDelayCall(() => 
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
        }, 2);
    }

    private void HandleNewPlayerState(State oldState, State newState)
    {
        if (kinematicStates.Contains(newState))
        {
            // For some states, we don't want other players being able to push the player around
            // so we make them kinematic
            rb2d.isKinematic = true;
        } else if (kinematicStates.Contains(oldState))
        {
            // If we were kinematic last state, make not kinematic
            rb2d.isKinematic = false;
        }

        if (newState == State.NormalMovement)
        {
            StartNormalMovement();
        } else if (rotateOnlyStates.Contains(newState))
        {
            // Only snap on possession states
            StartRotateOnly(newState == State.Possession || newState == State.ChargeShot);
        } else if (newState == State.FrozenAfterGoal)
        {
            StopAllMovement(true);
        } else if (externalControlStates.Contains(newState))
        {
            StopAllMovement(false);
        }
    }
}
