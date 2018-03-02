using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IC = InControl;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDashBehavior : MonoBehaviour {
    public float chargeRate   = 1.0f;
    public float dashDuration = 0.5f;

    PlayerMovement playerMovement  = null;
    IC.InputDevice input           = null;
    Rigidbody2D    rb              = null;
    Coroutine      chargeCoroutine = null;
    Coroutine      dashCoroutine   = null;
    float          dashSpeed       = 0.0f;

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

        if (chargeCoroutine == null && Input.GetKeyDown(KeyCode.X)) {
            StartCharging();
        }

        if (chargeCoroutine != null && Input.GetKeyUp(KeyCode.X)) {
            StopCharging();
            dashCoroutine = StartCoroutine(Dash());
        }
    }

    void StartCharging() {
        playerMovement.StopAllMovement();
        rb.velocity = Vector2.zero;

        dashSpeed = playerMovement.movementSpeed;
        chargeCoroutine = StartCoroutine(Charge());
    }

    void StopCharging() {
        StopCoroutine(chargeCoroutine);
        chargeCoroutine = null;
    }

    IEnumerator Charge() {
        while (true) {
            dashSpeed += chargeRate * Time.deltaTime;

            // Continue updating direction to indicate charge direction.
            var direction = new Vector2(input.LeftStickX, input.LeftStickY);
            if (direction != Vector2.zero) rb.rotation = Vector2.SignedAngle(Vector2.right, direction);

            yield return null;
        }
    }

    IEnumerator Dash() {
        var direction = (Vector2)(Quaternion.AngleAxis(rb.rotation, Vector3.forward) * Vector3.right);
        var startTime = Time.time;

        while (Time.time - startTime < dashDuration) {
            rb.velocity = direction * dashSpeed;

            yield return null;
        }

        dashCoroutine = null;
        playerMovement.StartPlayerMovement();

        yield return null;
    }
}
