﻿using System.Collections;
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
    public bool onlyStunBallCarriers = true;

    PlayerStateManager stateManager;
    PlayerMovement     playerMovement;
    Player player;
    IC.InputDevice     input;
    Rigidbody2D        rb;
    Coroutine          chargeCoroutine;
    Coroutine          dashCoroutine;
    float dashSpeed;
    BallCarrier carrier;

    void Start() {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        input          = playerMovement.GetInputDevice();
        rb             = this.EnsureComponent<Rigidbody2D>();
        stateManager   = this.EnsureComponent<PlayerStateManager>();
        player = this.EnsureComponent<Player>();
        carrier = this.EnsureComponent<BallCarrier>();
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

    Ball TrySteal(Player otherPlayer) {
        var otherCarrier = otherPlayer.gameObject.GetComponent<BallCarrier>();
        return otherCarrier?.ball;
    }

    void Stun(Player otherPlayer) {
        var otherStun = otherPlayer.GetComponent<PlayerStun>();
        var otherStateManager = otherPlayer.GetComponent<PlayerStateManager>();
        if (otherStun != null && otherStateManager != null) {
            otherStateManager.AttemptStun(
                () => otherStun.StartStun(rb.velocity * stealKnockbackPercentage),
                otherStun.StopStunned);
        }
    }

    void StunAndSteal(GameObject otherGameObject) {
        if (!stateManager.IsInState(State.Dash)) {
            return;
        }

        var otherPlayer = GetAssociatedPlayer(otherGameObject);
        if (otherPlayer != null &&
            otherPlayer.team.teamColor != player.team.teamColor) {
            var ball = TrySteal(otherPlayer);
            Stun(otherPlayer);
            if (ball != null) {
                stateManager.AttemptPossession(() => carrier.StartCarryingBall(ball),
                                               carrier.DropBall);
            }
        }
    }

    Player GetAssociatedPlayer(GameObject gameObject) {
        var ball = gameObject.GetComponent<Ball>();
        if (ball != null) {
            return (ball.owner == null) ? null : ball.owner.GetComponent<Player>();
        }

        if (onlyStunBallCarriers) {
            return null;
        } else {
            return gameObject.GetComponent<Player>();
        }

    }

    public void OnCollisionEnter2D(Collision2D collision) {
        StunAndSteal(collision.gameObject);
    }
}
