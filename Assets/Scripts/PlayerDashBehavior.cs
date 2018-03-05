using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IC = InControl;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDashBehavior : MonoBehaviour {
    public IC.InputControlType dashButton    = IC.InputControlType.Action2;
    public float               maxChargeTime = 1.0f;
    public float               chargeRate    = 1.0f;
    public float               dashPower     = 0.1f;
    public float               dashDuration  = 0.0f;

    PlayerMovement playerMovement;
    IC.InputDevice input;
    Rigidbody2D    rb;
    Coroutine      chargeCoroutine;
    Coroutine      dashCoroutine;

    void Start() {
        playerMovement = GetComponent<PlayerMovement>();
        input          = playerMovement.GetInputDevice();
        rb             = GetComponent<Rigidbody2D>();

        // playerMovement should never be null, bc of RequireComponent
        playerMovement.StartMovementFunction += ResetStateMachine;
        playerMovement.StopMovementFunction += ResetStateMachine;
    }

    public void ResetStateMachine() {
        if (chargeCoroutine != null) {
            StopCoroutine(chargeCoroutine);
            chargeCoroutine = null;
        }
        if (dashCoroutine != null) {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }
    }

    void Update() {
        if (input == null) {
            input = playerMovement.GetInputDevice();
            return;
        }

        var ballCarrier = GetComponent<BallCarrier>();
        if (ballCarrier.IsCarryingBall) {
            return;
        }
        
        // Do nothing if currently in dashing state.
        if (dashCoroutine != null) return;

        if (chargeCoroutine == null && input.GetControl(dashButton).WasPressed) {
            chargeCoroutine = StartCoroutine(Charge());
        }
    }

    IEnumerator Charge() {
        // Take control over player movement.
        playerMovement.StopAllMovement();

        var dashSpeed       = 1.0f;
        var startChargeTime = Time.time;

        while (true) {
            dashSpeed += chargeRate * Time.deltaTime;

            // Continue updating direction to indicate charge direction.
	    playerMovement.RotatePlayer();

            // Start dash and terminate Charge coroutine.
            if (input.GetControl(dashButton).WasReleased || (Time.time - startChargeTime) >= maxChargeTime) {
                dashCoroutine   = StartCoroutine(Dash(dashSpeed));
                chargeCoroutine = null;
                yield break;
            }

            yield return null;
        }
    }

    IEnumerator Dash(float dashSpeed) {
        var direction = (Vector2)(Quaternion.AngleAxis(rb.rotation, Vector3.forward) * Vector3.right);
        var startTime = Time.time;

        // Apply scaled dash speed on top of base movement speed.
        dashSpeed = playerMovement.movementSpeed + Mathf.Pow(dashSpeed, dashPower);

        while (Time.time - startTime < dashDuration) {
            rb.velocity = direction * dashSpeed;

            yield return null;
        }

        // Return normal movement control to player.
        playerMovement.StartPlayerMovement();
        dashCoroutine = null;

        yield break;
    }
}
