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

    PlayerMovement playerMovement  = null;
    IC.InputDevice input           = null;
    Rigidbody2D    rb              = null;
    Coroutine      chargeCoroutine = null;
    Coroutine      dashCoroutine   = null;
    float          startChargeTime = 0.0f;
    float          dashSpeed       = 1.0f;

    void Start() {
        playerMovement = GetComponent<PlayerMovement>();
        input          = playerMovement.GetInputDevice();
        rb             = GetComponent<Rigidbody2D>();
    }

    void Update() {
        if (input == null) {
            input = playerMovement.GetInputDevice();
            return;
        }

        // Do nothing if currently in dashing state.
        if (dashCoroutine != null) return;

        var control = input.GetControl(dashButton);

        if (chargeCoroutine == null && control.WasPressed) {
            chargeCoroutine = StartCoroutine(Charge());
        }

        if (
            chargeCoroutine != null && (
                // Start dash if player releases dash button, or...
                control.WasReleased ||
                // If charge time exceeds maximum allowed.
                (Time.time - startChargeTime) >= maxChargeTime
            )
        ) {
            dashCoroutine = StartCoroutine(Dash());
        }
    }

    IEnumerator Charge() {
        // Take control over player movement.
        playerMovement.StopAllMovement();
        rb.velocity = Vector2.zero;
        dashSpeed   = 1.0f;

        startChargeTime = Time.time;

        while (true) {
            dashSpeed += chargeRate * Time.deltaTime;

            // Continue updating direction to indicate charge direction.
            var direction = new Vector2(input.LeftStickX, input.LeftStickY);
            if (direction != Vector2.zero) rb.rotation = Vector2.SignedAngle(Vector2.right, direction);

            yield return null;
        }
    }

    IEnumerator Dash() {
        // Stop charging the dash.
        StopCoroutine(chargeCoroutine);
        chargeCoroutine = null;

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

        yield return null;
    }
}
