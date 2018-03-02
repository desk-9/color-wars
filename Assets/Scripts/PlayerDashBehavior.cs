using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IC = InControl;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDashBehavior : MonoBehaviour {
    public float chargeRate = 1.0f;
    public float dashSpeed  = 30.0f;
    public float scale      = 2.0f;

    PlayerMovement playerMovement  = null;
    IC.InputDevice input           = null;
    Rigidbody2D    rb              = null;
    Coroutine      chargeCoroutine = null;
    Coroutine      dashCoroutine   = null;
    float          chargeDistance  = 0.0f;

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
        Debug.LogFormat(this, "{0}: charging...", name);

        playerMovement.StopAllMovement();
        rb.velocity = Vector2.zero;

        chargeDistance = 0.0f;
        chargeCoroutine = StartCoroutine(Charge());
    }

    void StopCharging() {
        Debug.LogFormat(this, "{0}: stop charging!", name);

        StopCoroutine(chargeCoroutine);
        chargeCoroutine = null;
    }

    IEnumerator Charge() {
        while (true) {
            chargeDistance += chargeRate * Time.deltaTime;

            // Continue updating direction to indicate charge direction.
            var direction = new Vector2(input.LeftStickX, input.LeftStickY);
            if (direction != Vector2.zero) rb.rotation = Vector2.SignedAngle(Vector2.right, direction);

            yield return null;
        }
    }

    IEnumerator Dash() {
        var direction = (Vector2)(Quaternion.AngleAxis(rb.rotation, Vector3.forward) * Vector3.right);
        var source    = (Vector2)transform.position;
        var target    = source + direction * chargeDistance * scale;
        var startTime = Time.time;

        Debug.LogFormat(this, "{0}: Dash with distance: {1}!", name, (target - source).magnitude);

        while ((Vector2)transform.position != target) {
            var step = (Time.time - startTime) * dashSpeed;

            rb.MovePosition(Vector2.Lerp(source, target, step));

            yield return null;
        }

        Debug.LogFormat(this, "{0}: Exit dash!", name);

        dashCoroutine = null;
        playerMovement.StartPlayerMovement();

        yield return null;
    }
}
