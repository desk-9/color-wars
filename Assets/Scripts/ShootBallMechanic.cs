using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using IC = InControl;

public class ShootBallMechanic : MonoBehaviour {

    public IC.InputControlType shootButton = IC.InputControlType.Action1;
    public float forcedShotTime;
    public float baseShotSpeed = 1.0f;
    public float chargeRate = 1.0f;
    public float shotPower = 1.1f;
    public GameObject circularTimerPrefab;
    public Vector2 circleTimerScale;
    CircularTimer circularTimer;
     public float chargedBallPercent = 0.4f;
    public float chargedBallMassFactor = 1;
    public bool chargedBallStuns = false;

    public GameObject chargeEffect;

    PlayerMovement playerMovement;
    PlayerStateManager stateManager;
    Coroutine shootTimer;
    BallCarrier ballCarrier;
    GameObject effect;
    TeamManager team;
    Player player;
    Goal goal;
    Player teamMate;

    float shotSpeed = 1.0f;
    float elapsedTime = 0.0f;

    float maxShotSpeed;

    void Start() {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        ballCarrier = this.EnsureComponent<BallCarrier>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        player = this.EnsureComponent<Player>();
        goal = GameObject.FindObjectOfType<Goal>();
        stateManager.CallOnStateEnter(
                                      State.Posession, StartTimer);
        stateManager.CallOnStateExit(
            State.Posession, () => StopChargeShot());

        if (chargeEffect != null) {
            var ps = chargeEffect.GetComponent<ParticleSystem>();
            var main = ps.main;
            if (main.duration != forcedShotTime) {
                main.duration = forcedShotTime;
            }
        }
        circularTimer = Instantiate(
            circularTimerPrefab, transform).GetComponent<CircularTimer>();
        circularTimer.transform.localScale = circleTimerScale;

        ballCarrier.chargedBallStuns = chargedBallStuns;
        var ball = GameObject.FindObjectOfType<Ball>();
        if (ball != null) {
            ball.chargedMassFactor = chargedBallMassFactor;
        }
        maxShotSpeed = baseShotSpeed + Mathf.Pow((1 + forcedShotTime * chargeRate), shotPower);
    }

    void StartTimer() {
        shootTimer = StartCoroutine(ShootTimer());
    }

    IEnumerator ShootTimer() {
        // circularTimer?.StartTimer(forcedShotTime, delegate{});

        elapsedTime = 0.0f;
        shotSpeed = baseShotSpeed;
        while (elapsedTime < forcedShotTime) {
            elapsedTime += Time.deltaTime;
            var inputDevice = playerMovement.GetInputDevice();
            if (inputDevice.GetControl(shootButton).WasPressed) {
                shootTimer = StartCoroutine(ChargeShot(shootButton));
                yield break;
            }
            yield return null;
        }
        Utility.TutEvent("BallPickupTimeout", this);
        Shoot();
    }

    IEnumerator ChargeShot(IC.InputControlType button) {
        effect = Instantiate(chargeEffect, transform.position, transform.rotation, transform);

        var inputDevice = playerMovement.GetInputDevice();

        while (elapsedTime < forcedShotTime) {
            elapsedTime += Time.deltaTime;
            shotSpeed += chargeRate * Time.deltaTime;

            if (inputDevice.GetControl(button).WasReleased) {
                shotSpeed = baseShotSpeed + Mathf.Pow(shotSpeed, shotPower);
                Shoot();
                yield break;
            }

            yield return null;
        }

        shotSpeed = baseShotSpeed + Mathf.Pow(shotSpeed, shotPower);
        Shoot();
    }

    void Shoot() {
        Utility.TutEvent("Shoot", this);
        shootTimer = null;
        var ball = ballCarrier.ball;
        var shotDirection = ball.transform.position - transform.position;

        shootTimer = null;
        var ballRigidBody = ball.EnsureComponent<Rigidbody2D>();
        if (shotSpeed / maxShotSpeed >= 0.45f) {
            Utility.TutEvent("ShootCharge", this);
        }
        if (shotSpeed / maxShotSpeed >= chargedBallPercent) {
            ball.charged = true;
        }
        ballRigidBody.velocity = shotDirection.normalized * shotSpeed;
        stateManager.CurrentStateHasFinished();
    }

    void StopChargeShot() {
        // circularTimer?.StopTimer();
        if (shootTimer != null) {
            StopCoroutine(shootTimer);
            shootTimer = null;
        }
        if (effect != null) {
            Destroy(effect);
        }
        playerMovement.freezeRotation = false;
    }

}
