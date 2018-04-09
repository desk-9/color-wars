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
    Player teamMate;

    float shotSpeed = 1.0f;
    float elapsedTime = 0.0f;

    float maxShotSpeed;

    void Start() {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        ballCarrier = this.EnsureComponent<BallCarrier>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        stateManager.CallOnStateEnter(State.Posession, StartTimer);
        stateManager.CallOnStateExit(
            State.Posession, () => StopChargeShot());

        GameModel.instance.nc.CallOnMessageIfSameObject(
            Message.PlayerPressedShoot, ShootPressed, gameObject);
        GameModel.instance.nc.CallOnMessageIfSameObject(
            Message.PlayerReleasedShoot, ShootReleased, gameObject);
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

    void ShootPressed() {
        if (stateManager.IsInState(State.Posession)) {
            if (shootTimer != null) {
                StopCoroutine(shootTimer);
            }
            shootTimer = StartCoroutine(ChargeShot());
        }
    }

    void ShootReleased() {
        if (shootTimer != null) {
            StopCoroutine(shootTimer);
            shotSpeed = baseShotSpeed + Mathf.Pow(shotSpeed, shotPower);
            Shoot();
        }
    }

    IEnumerator ShootTimer() {
        // circularTimer?.StartTimer(forcedShotTime, delegate{});

        elapsedTime = 0.0f;
        shotSpeed = baseShotSpeed;
        while (elapsedTime < forcedShotTime) {
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Utility.TutEvent("BallPickupTimeout", this);
        Shoot();
    }

    IEnumerator ChargeShot() {
        effect = Instantiate(chargeEffect, transform.position, transform.rotation, transform);

        while (elapsedTime < forcedShotTime) {
            elapsedTime += Time.deltaTime;
            shotSpeed += chargeRate * Time.deltaTime;

            yield return null;
        }

        shotSpeed = baseShotSpeed + Mathf.Pow(shotSpeed, shotPower);
        Shoot();
    }

    void Shoot() {
        Utility.TutEvent("Shoot", this);
        AudioManager.instance.ShootBallSound.Play(.5f);
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
