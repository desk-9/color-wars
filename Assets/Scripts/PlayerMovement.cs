using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;
using UtilityExtensions;


public class PlayerMovement : MonoBehaviour, IPlayerMovement {

    public float movementSpeed;
    public float rotationSpeed = 1080;
    public bool freezeRotation = false;
    public float maxAwayFromBallAngle = 10f;

    public bool instantRotation {get; set;} = true;
    Rigidbody2D rb2d;
    Coroutine playerMovementCoroutine = null;
    PlayerStateManager stateManager;

    public Vector2 lastDirection = Vector2.zero;

    const float minBallForceRotationTime = 0.05f;

    void StartPlayerMovement() {
        playerMovementCoroutine = StartCoroutine(Move());
    }

    public void StopAllMovement() {
        if (playerMovementCoroutine != null) {
            StopCoroutine(playerMovementCoroutine);
            rb2d.velocity = Vector2.zero;
        }
    }

    public void FreezePlayer() {
        rb2d.isKinematic = true;
    }

    public void UnFreezePlayer() {
        rb2d.isKinematic = false;
    }

    public void RotatePlayer () {
        if (freezeRotation) {
            return;
        }
        var direction = lastDirection;
        if (direction != Vector2.zero) {
            // Only do if nonzero, otherwise [SignedAngle] returns 90 degrees
            // and player snaps to up direction
            if (instantRotation) {
                rb2d.rotation = Vector2.SignedAngle(Vector2.right, direction);
            } else {
                var maxAngleChange = Vector2.SignedAngle(transform.right, direction);
                var sign = Mathf.Sign(maxAngleChange);
                var speedChange = rotationSpeed * Time.deltaTime;
                var actualChange = sign * Mathf.Min(Mathf.Abs(maxAngleChange), speedChange);
                var finalRotation = rb2d.rotation + actualChange;
                if (finalRotation <= 0) {
                    finalRotation = 360 - Mathf.Repeat(-finalRotation, 360);
                }
                finalRotation = Mathf.Repeat(finalRotation, 360);
                var ballCarrier = GetComponent<BallCarrier>();
                if (ballCarrier != null && ballCarrier.ball != null
                    && (Time.time - ballCarrier.timeCarryStarted) >= minBallForceRotationTime) {
                    var ball = ballCarrier.ball;
                    var ballDirection = (ball.transform.position - transform.position).normalized;
                    var unitFinal = Quaternion.AngleAxis(finalRotation, Vector3.forward) * Vector2.right;
                    float angleDifference = Vector2.SignedAngle(ballDirection, unitFinal);
                    if (Mathf.Abs(angleDifference) >= maxAwayFromBallAngle) {
                        finalRotation =
                            Vector2.SignedAngle(Vector2.right, ballDirection)
                            + Mathf.Sign(angleDifference) * maxAwayFromBallAngle;
                    }
                }
                if (finalRotation <= 0) {
                    finalRotation = 360 - Mathf.Repeat(-finalRotation, 360);
                }
                finalRotation = Mathf.Repeat(finalRotation, 360);
                rb2d.rotation = finalRotation;
            }
        }
    }

    IEnumerator Move () {
        float startTime = Time.time;
        yield return new WaitForFixedUpdate();

        while (true) {
            rb2d.velocity = movementSpeed * lastDirection;
            // TODO: TUTORIAL
            if (lastDirection.magnitude > 0.1f) {
                if (Time.time - startTime > 0.75f) {
                    GameModel.instance.nc.NotifyStringEvent("MoveTutorial", this.gameObject);
                }
            } else {
                startTime = Time.time;
            }

            RotatePlayer();
            yield return null;
        }
    }

    // Use this for initialization
    void Start () {
        rb2d = this.EnsureComponent<Rigidbody2D>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        GameModel.instance.nc.CallOnMessageWithSender(
            Message.PlayerStick, playerPair => {
                var pair = playerPair as Tuple<Vector2, GameObject>;
                var player = pair?.Item2;
                if (pair != null && this != null && player == this.gameObject) {
                    lastDirection = pair.Item1;
                }
            });
        stateManager.AttemptNormalMovement(StartPlayerMovement, StopAllMovement);
    }
}
