using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IC = InControl;
using UtilityExtensions;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDashBehavior : MonoBehaviour {
    public IC.InputControlType dashButton = IC.InputControlType.Action2;
    public GameObject          chargeEffect;
    public GameObject          dashEffect;
    public float               maxChargeTime = 1.0f;
    public float               chargeRate    = 1.0f;
    public float               dashPower     = 0.1f;
    public float               dashDuration  = 0.0f;

    PlayerStateManager stateManager;
    PlayerMovement     playerMovement;
    IC.InputDevice     input;
    Rigidbody2D        rb;
    Coroutine          chargeCoroutine;
    Coroutine          dashCoroutine;
    GameObject         effect;

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
        rb.constraints = RigidbodyConstraints2D.FreezePosition;

        // Start charging effect.
        effect = Instantiate(chargeEffect, transform.position, Quaternion.identity, transform);
    }

    void StopChargeDash() {
        if (chargeCoroutine != null) {
            StopCoroutine(chargeCoroutine);
            chargeCoroutine = null;

            // Release player from position lock.
            rb.constraints = RigidbodyConstraints2D.None;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Stop charging effect.
            Destroy(effect);
        }
    }

    IEnumerator Charge() {
        var dashSpeed       = 1.0f;
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

    IEnumerator Dash(float dashSpeed) {
        var direction = (Vector2)(Quaternion.AngleAxis(rb.rotation, Vector3.forward) * Vector3.right);
        var startTime = Time.time;

        // Apply scaled dash speed on top of base movement speed.
        dashSpeed = playerMovement.movementSpeed + Mathf.Pow(dashSpeed, dashPower);

        while (Time.time - startTime < dashDuration) {
            rb.velocity = direction * dashSpeed;

            yield return null;
        }

        dashCoroutine = null;
        stateManager.CurrentStateHasFinished();
    }
}
