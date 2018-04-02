using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IC = InControl;
using UtilityExtensions;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerDashBehavior : MonoBehaviour {
    public GameObject dashEffectPrefab;
    public GameObject dashAimerPrefab;
    public IC.InputControlType dashButton = IC.InputControlType.Action2;
    public float stealKnockbackPercentage = 0.8f;
    public bool onlyStunBallCarriers = true;
    public bool onlyStealOnBallHit = false;
    public string[] stopDashOnCollisionWith;
    public float maxChargeTime = 1.0f;
    public float chargeRate = 1.0f;
    public float dashSpeed = 50.0f;
    public float cooldown = 0.5f;
    public float stealShakeAmount = .7f;
    public float stealShakeDuration = .05f;

    PlayerStateManager stateManager;
    PlayerMovement playerMovement;
    Player player;
    Rigidbody2D        rb;
    Coroutine          chargeCoroutine;
    Coroutine          dashCoroutine;
    PlayerTronMechanic tronMechanic;
    BallCarrier carrier;
    GameObject dashAimer;
    float lastDashTime;
    CameraShake cameraShake;

    void Start() {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        rb             = this.EnsureComponent<Rigidbody2D>();
        stateManager   = this.EnsureComponent<PlayerStateManager>();
        carrier        = this.EnsureComponent<BallCarrier>();
        tronMechanic = this.EnsureComponent<PlayerTronMechanic>();
        cameraShake = GameObject.FindObjectOfType<CameraShake>();
    }

    void Awake() {
        player = this.EnsureComponent<Player>();
    }

    public void SetPrefabColors() {
        if (player.team != null) {
            var chargeEffectSpawner = this.FindEffect(EffectType.DashCharge);
            dashEffectPrefab = player.team.resources.dashEffectPrefab;
            chargeEffectSpawner.effectPrefab = player.team.resources.dashChargeEffectPrefab;
            dashAimerPrefab = player.team.resources.dashAimerPrefab;
        }
    }

    void Update() {
        var input = playerMovement.GetInputDevice();

        if (Time.time - lastDashTime < cooldown) return;

        if (input != null && input.GetControl(dashButton).WasPressed) {
            stateManager.AttemptDashCharge(StartChargeDash, StopChargeDash);
        }
    }

    void StartChargeDash() {
        chargeCoroutine = StartCoroutine(Charge());

        // Lock Player at current position when charging.
        playerMovement.FreezePlayer();

        dashAimer = Instantiate(dashAimerPrefab, transform.position, transform.rotation, transform);
    }

    void StopChargeDash() {
        if (chargeCoroutine != null) {
            StopCoroutine(chargeCoroutine);
            chargeCoroutine = null;
            playerMovement.UnFreezePlayer();

            Destroy(dashAimer);
        }
    }

    IEnumerator Charge() {
        var startChargeTime = Time.time;
        var chargeAmount    = 0.0f;

        while (true) {
            chargeAmount += chargeRate * Time.deltaTime;

            // Continue updating direction to indicate charge direction.
            playerMovement.RotatePlayer();

            var input = playerMovement.GetInputDevice();

            if (
                input != null && (
                    input.GetControl(dashButton).WasReleased
                    || (Time.time - startChargeTime) >= maxChargeTime
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
        if (tronMechanic.layWallOnDash) {
            tronMechanic.PlaceWallAnchor();
        }
    }

    void StopDash() {
        if (dashCoroutine != null) {
            StopCoroutine(dashCoroutine);
            dashCoroutine = null;
        }
    }

    IEnumerator Dash(float chargeAmount) {
        var dashDuration = Mathf.Min(chargeAmount, 0.5f);
        AudioManager.instance.DashSound.Play();

        Utility.TutEvent("Dash", this);
        if (dashDuration > 0.25f) {
            Utility.TutEvent("DashCharge", this);
        }

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

    Ball TrySteal(Player otherPlayer) {
        var otherCarrier = otherPlayer.gameObject.GetComponent<BallCarrier>();
        return otherCarrier?.ball;
    }
    void Stun(Player otherPlayer) {
        var otherStun = otherPlayer.GetComponent<PlayerStun>();
        var otherStateManager = otherPlayer.GetComponent<PlayerStateManager>();
        if (otherStun != null && otherStateManager != null) {
            cameraShake.shakeAmount = stealShakeAmount;
            cameraShake.shakeDuration = stealShakeDuration;
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
                AudioManager.instance.StealSound.Play(.5f);
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

    public void OnTriggerStay2D(Collider2D collider) {
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

    public void OnCollisionStay2D(Collision2D collision) {
        HandleCollision(collision.gameObject);
    }
}
