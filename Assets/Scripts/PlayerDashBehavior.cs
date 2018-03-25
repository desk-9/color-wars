using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IC = InControl;
using UtilityExtensions;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDashBehavior : MonoBehaviour {
    // ==================== //
    // === OLD BEHAVIOR === //
    // ==================== //

    public IC.InputControlType dashButton = IC.InputControlType.Action2;
    public BoxCollider2D dashGrabField;
    public float stealKnockbackPercentage = 0.8f;
    public float maxChargeTime = 1.0f;
    public float dashDuration = 0.25f;
    public float chargeRate = 1.0f;
    public float dashPower = 0.1f;
    public bool onlyStunBallCarriers = true;
    public bool onlyStealOnBallHit = false;
    public string[] stopDashOnCollisionWith;
    public bool layWallOnDash;


    PlayerStateManager stateManager;
    PlayerMovement     playerMovement;
    Player player;
    Rigidbody2D        rb;
    Coroutine          chargeCoroutine;
    Coroutine          dashCoroutine;
    PlayerTronMechanic tronMechanic;
    BallCarrier carrier;
    GameObject dashEffect;
    void Start() {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        rb             = this.EnsureComponent<Rigidbody2D>();
        stateManager   = this.EnsureComponent<PlayerStateManager>();
        player         = this.EnsureComponent<Player>();
        carrier        = this.EnsureComponent<BallCarrier>();
        tronMechanic = this.EnsureComponent<PlayerTronMechanic>();
        dashGrabField.enabled = false;
    }

    public void SetPrefabColors() {
        if (player.team != null) {
            var name = player.team.teamColor.name;
            var chargeEffectSpawner = this.FindEffect(EffectType.DashCharge);
            if (name == "Pink") {
                dashEffectPrefab = pinkDashEffectPrefab;
                chargeEffectSpawner.effectPrefab = pinkChargeEffectPrefab;
            } else if (name == "Blue") {
                dashEffectPrefab = blueDashEffectPrefab;
                chargeEffectSpawner.effectPrefab = blueChargeEffectPrefab;
            }
        }
    }

    void Update() {
        var input = playerMovement.GetInputDevice();

        if (Time.time - lastDashTime < cooldown) return;
        // Utility.Print(this.name, input, input?.GetControl(IC.InputControlType.Action1).WasPressed);
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
            Destroy(dashEffect, 1.0f);
            dashGrabField.enabled = false;
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
            if (
                input != null && (
                    input.GetControl(dashButton).WasReleased
                    || (Time.time - startChargeTime) >=
                    newMaxChargeTime
                )
            ) {
                stateManager.AttemptDash(() => StartDash(chargeAmount), StopDash);
                yield break;
            }

            yield return null;
        }
    }

    void StartDash(float chargeAmount) {
        dashCoroutine = StartCoroutine(Dash(chargeAmount));
        lastDashTime = Time.time;
        if (layWallOnDash) {
            tronMechanic.PlaceWallAnchor();
        }
    }

    void StopDash() {
        if (dashCoroutine != null) {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;

            if (layWallOnDash) {
                tronMechanic.PlaceCurrentWall();
            }
        }
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
        bool hitBall = otherGameObject.GetComponent<Ball>() != null;
        var otherPlayer = GetAssociatedPlayer(otherGameObject);
        if (otherPlayer != null &&
            (otherPlayer.team?.teamColor != player.team?.teamColor
             || otherPlayer.team == null || player.team == null) ) {
            var ball = TrySteal(otherPlayer);

            bool shouldSteal = ball != null && (!onlyStealOnBallHit || hitBall);
            if (shouldSteal || (ball == null && !onlyStunBallCarriers)) {
                Stun(otherPlayer);
            }

            if (shouldSteal) {
                Utility.TutEvent("Steal", this);
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

    public void OnTriggerEnter2D(Collider2D collider) {
        StunAndSteal(collider.gameObject);
    }

    void HandleCollision(GameObject other) {
        if (!stateManager.IsInState(State.Dash)) {
            return;
        }

        var layerMask = LayerMask.GetMask(stopDashOnCollisionWith);
        if (layerMask == (layerMask | 1 << other.layer)) {
            stateManager.CurrentStateHasFinished();
        } else {
            StunAndSteal(other);
        }

    }

    public void OnCollisionEnter2D(Collision2D collision) {
        HandleCollision(collision.gameObject);
    }

    void OnCollisionStay2D(Collision2D collision) {
        HandleCollision(collision.gameObject);
    }



    // ==================== //
    // === NEW BEHAVIOR === //
    // ==================== //

    public GameObject blueDashEffectPrefab;
    public GameObject pinkDashEffectPrefab;
    public GameObject blueChargeEffectPrefab;
    public GameObject pinkChargeEffectPrefab;
    public GameObject dashEffectPrefab;
    public float newMaxChargeTime = 1.0f;
    public float newChargeRate    = 1.0f;
    public float dashSpeed        = 50.0f;
    public float cooldown         = 0.5f;

    float lastDashTime;

    IEnumerator Dash(float chargeAmount) {
        var dashDuration = Mathf.Min(chargeAmount, 0.5f);
        Utility.TutEvent("Dash", this);
        if (dashDuration > 0.25f) {
            Utility.TutEvent("DashCharge", this);
        }
        // Set duration of particle system for each dash trail.
        dashEffect = Instantiate(dashEffectPrefab, transform.position, transform.rotation, transform);
        foreach (var ps in dashEffect.GetComponentsInChildren<ParticleSystem>()) {
            ps.Stop();
            var main = ps.main;
            main.duration = dashDuration;
            ps.Play();
        }

        var direction = (Vector2)(Quaternion.AngleAxis(rb.rotation, Vector3.forward) * Vector3.right);
        var startTime = Time.time;

        dashGrabField.enabled = true;

        while (Time.time - startTime <= dashDuration) {
            rb.velocity = direction * dashSpeed * (1.0f + chargeAmount);

            yield return null;
        }

        foreach (var ps in dashEffect.GetComponentsInChildren<ParticleSystem>()) {
            ps.Stop();
        }

        dashGrabField.enabled = false;
        Destroy(dashEffect, 1.0f);
        dashCoroutine = null;
        if (layWallOnDash) {
            tronMechanic.PlaceCurrentWall();
        }
        stateManager.CurrentStateHasFinished();
    }
}
