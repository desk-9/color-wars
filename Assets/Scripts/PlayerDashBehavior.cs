using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IC = InControl;
using UtilityExtensions;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDashBehavior : MonoBehaviour {
    public bool useNewBehavior = true;

    // ==================== //
    // === OLD BEHAVIOR === //
    // ==================== //

    public IC.InputControlType dashButton = IC.InputControlType.Action2;
    public float stealKnockbackPercentage = 0.8f;
    public float maxChargeTime = 1.0f;
    public float dashDuration = 0.25f;
    public float chargeRate = 1.0f;
    public float dashPower = 0.1f;
    public bool onlyStunBallCarriers = true;
    public bool onlyStealOnBallHit = false;


    PlayerStateManager stateManager;
    PlayerMovement     playerMovement;
    Player player;
    Rigidbody2D        rb;
    Coroutine          chargeCoroutine;
    Coroutine          dashCoroutine;
    BallCarrier carrier;

    void Start() {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        rb             = this.EnsureComponent<Rigidbody2D>();
        stateManager   = this.EnsureComponent<PlayerStateManager>();
        player = this.EnsureComponent<Player>();
        carrier = this.EnsureComponent<BallCarrier>();
    }

    void Update() {
        var input = playerMovement.GetInputDevice();

        if (useNewBehavior) {
            if (Time.time - lastDashTime < cooldown) return;
        }
        if (input != null && input.GetControl(dashButton).WasPressed) {
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
        var startChargeTime = Time.time;
        var chargeAmount    = 0.0f;
        var dashSpeed       = 1.0f;

        while (true) {
            chargeAmount += newChargeRate * Time.deltaTime;
            dashSpeed    += chargeRate    * Time.deltaTime;

            // Continue updating direction to indicate charge direction.
            playerMovement.RotatePlayer();

            var input = playerMovement.GetInputDevice();
            // Start dash and terminate Charge coroutine.
            if (input != null && (
                    input.GetControl(dashButton).WasReleased
                    || (Time.time - startChargeTime) >=
                    (useNewBehavior ? newMaxChargeTime : maxChargeTime))) {
                if (useNewBehavior) stateManager.AttemptDash(() => StartNewDash(chargeAmount), StopDash);
                else                stateManager.AttemptDash(() => StartDash(dashSpeed)      , StopDash);

                yield break;
            }

            yield return null;
        }
    }

    void StartDash(float dashSpeed) {
        dashCoroutine = StartCoroutine(Dash(dashSpeed));
    }

    void StartNewDash(float chargeAmount) {
        dashCoroutine = StartCoroutine(NewDash(chargeAmount));
        lastDashTime = Time.time;
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
        speed = playerMovement.movementSpeed + Mathf.Pow(speed, dashPower);

        while (Time.time - startTime < dashDuration) {
            rb.velocity = direction * speed;

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
        bool hitBall = otherGameObject.GetComponent<Ball>() != null;
        var otherPlayer = GetAssociatedPlayer(otherGameObject);
        if (otherPlayer != null &&
            otherPlayer.team.teamColor != player.team.teamColor) {
            var ball = TrySteal(otherPlayer);

            bool shouldSteal = ball != null && (!onlyStealOnBallHit || hitBall);
            if (shouldSteal || (ball == null && !onlyStunBallCarriers)) {
                Stun(otherPlayer);
            }

            if (shouldSteal) {
                stateManager.AttemptPossession(
                    () => carrier.StartCarryingBall(ball), carrier.DropBall);
            }
        }
    }

    Player GetAssociatedPlayer(GameObject gameObject) {
        var ball = gameObject.GetComponent<Ball>();
        if (ball != null) {
            return (ball.owner == null) ? null : ball.owner.GetComponent<Player>();
        }
        return gameObject.GetComponent<Player>();
    }

    public void OnCollisionEnter2D(Collision2D collision) {
        StunAndSteal(collision.gameObject);
    }



    // ==================== //
    // === NEW BEHAVIOR === //
    // ==================== //

    public GameObject dashEffectPrefab;
    public float newMaxChargeTime = 1.0f;
    public float newChargeRate    = 1.0f;
    public float dashSpeed        = 50.0f;
    public float cooldown         = 0.5f;

    float lastDashTime;

    IEnumerator NewDash(float chargeAmount) {
        var dashDuration = Mathf.Min(chargeAmount, 0.5f);

        // Set duration of particle system for each dash trail.
        var dashEffect = Instantiate(dashEffectPrefab, transform.position, transform.rotation, transform);
        foreach (var ps in dashEffect.GetComponentsInChildren<ParticleSystem>()) {
            ps.Stop();
            var main = ps.main;
            main.duration = dashDuration;
            ps.Play();
        }

        var direction = (Vector2)(Quaternion.AngleAxis(rb.rotation, Vector3.forward) * Vector3.right);
        var startTime = Time.time;

        while (Time.time - startTime <= dashDuration) {
            rb.velocity = direction * dashSpeed * (1.0f + chargeAmount);

            yield return null;
        }

        foreach (var ps in dashEffect.GetComponentsInChildren<ParticleSystem>()) {
            ps.Stop();
        }

        Destroy(dashEffect, 1.0f);
        stateManager.CurrentStateHasFinished();
    }
}
