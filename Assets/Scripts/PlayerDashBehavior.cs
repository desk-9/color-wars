using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IC = InControl;
using UtilityExtensions;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDashBehavior : MonoBehaviour {
    public IC.InputControlType dashButton = IC.InputControlType.Action2;
    public float stealKnockbackPercentage = 0.8f;
    public float maxChargeTime = 1.0f;
    public float dashDuration = 0.0f;
    public float chargeRate = 1.0f;
    public float dashPower = 0.1f;

    PlayerStateManager stateManager;
    PlayerMovement     playerMovement;
    IC.InputDevice     input;
    Rigidbody2D        rb;
    Coroutine          chargeCoroutine;
    Coroutine          dashCoroutine;
    float dashSpeed;

    void Start() {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        input          = playerMovement.GetInputDevice();
        rb             = this.EnsureComponent<Rigidbody2D>();
        stateManager   = this.EnsureComponent<PlayerStateManager>();
    }

    void Update() {
        if (input == null) {
            input = playerMovement.GetInputDevice();
            return;
        }

        if (input.GetControl(dashButton).WasPressed) {
            stateManager.AttemptDashCharge(StartChargeDash, StopChargeDash);
        }
    }

    void StartChargeDash() {
        chargeCoroutine = StartCoroutine(Charge());

        // Lock Player at current position when charging.
        playerMovement.FreezePlayer();

    }

    void StopChargeDash() {
        if (chargeCoroutine != null) {
            StopCoroutine(chargeCoroutine);
            chargeCoroutine = null;

            // Release player from position lock.
            playerMovement.UnFreezePlayer();
        }
    }

    IEnumerator Charge() {
        dashSpeed = 1.0f;
        var startChargeTime = Time.time;

        while (true) {
            dashSpeed += chargeRate * Time.deltaTime;

            // Continue updating direction to indicate charge direction.
            playerMovement.RotatePlayer();

            // Start dash and terminate Charge coroutine.
            if (input.GetControl(dashButton).WasReleased || (Time.time - startChargeTime) >= maxChargeTime) {
                stateManager.AttemptDash(() => StartDash(dashSpeed), StopDash);
                yield break;
            }

            yield return null;
        }
    }

    void StartDash(float dashSpeed) {
        dashCoroutine = StartCoroutine(Dash(dashSpeed));
    }

    void StopDash() {
        if (dashCoroutine != null) {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }
    }

    IEnumerator Dash(float speed) {
        var direction = (Vector2)(Quaternion.AngleAxis(rb.rotation, Vector3.forward) * Vector3.right);
        var startTime = Time.time;

        // Apply scaled dash speed on top of base movement speed.
        dashSpeed = playerMovement.movementSpeed + Mathf.Pow(speed, dashPower);

        while (Time.time - startTime < dashDuration) {
            rb.velocity = direction * dashSpeed;

            yield return null;
        }

        dashCoroutine = null;
        stateManager.CurrentStateHasFinished();
    }

    void Steal(BallCarrier otherCarrier, PlayerStateManager otherStateManager) {
        var ball = otherCarrier.ball;
        var otherStun = otherStateManager.GetComponent<PlayerStun>();
        if (otherStun != null) {
            otherStateManager.AttemptStun(
                () => {
                    otherStun.StartStun();
                    var otherBody = otherCarrier.GetComponent<Rigidbody2D>();
                    if (otherBody != null) {
                        var magnitude = (rb.velocity.magnitude / Time.fixedDeltaTime)
                            * otherBody.mass;
                        var force = rb.velocity.normalized * magnitude;
                        otherBody.AddForce(force * stealKnockbackPercentage);
                    }
                },
                otherStun.StopStunned);
        }
        stateManager.CurrentStateHasFinished();
        var carrier = this.EnsureComponent<BallCarrier>();
        stateManager.AttemptPossession(
            () => carrier.StartCarryingBall(ball), carrier.DropBall);

    }

    void TrySteal(GameObject stealable) {
        if (!stateManager.IsInState(State.Dash)) {
            return;
        }
        var ball = stealable.GetComponent<Ball>();
        if (ball != null && ball.owner != null) {
            stealable = ball.owner.gameObject;
        }

        var otherCarrier = stealable.GetComponent<BallCarrier>();
        var otherStateManager = stealable.GetComponent<PlayerStateManager>();
        if (otherCarrier != null && otherStateManager != null && otherCarrier.ball != null) {
            Steal(otherCarrier, otherStateManager);
        }
    }

    public void OnCollisionEnter2D(Collision2D collision) {
        var stealable = collision.gameObject;
        TrySteal(stealable);
    }
}
