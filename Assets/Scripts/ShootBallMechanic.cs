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
    CircularTimer circularTimer;

    public GameObject chargeEffect;

    PlayerMovement playerMovement;
    PlayerStateManager stateManager;
    IC.InputDevice inputDevice;
    Coroutine shootTimer;
    BallCarrier ballCarrier;
    GameObject effect;

    float shotSpeed = 1.0f;
    float elapsedTime = 0.0f;

    void Start() {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        ballCarrier = this.EnsureComponent<BallCarrier>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        stateManager.CallOnStateEnter(
            State.Posession, () => shootTimer = StartCoroutine(ShootTimer()));
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
            circularTimerPrefab, transform.position,
            Quaternion.identity, transform).GetComponent<CircularTimer>();
    }

    IEnumerator ShootTimer() {
        circularTimer?.StartTimer(forcedShotTime, delegate{});


        elapsedTime = 0.0f;
        shotSpeed = baseShotSpeed;

        while (elapsedTime < forcedShotTime) {
            elapsedTime += Time.deltaTime;

            if (inputDevice.GetControl(shootButton).WasPressed) {
                shootTimer = StartCoroutine(ChargeShot(elapsedTime));
                yield break;
            }

            yield return null;
        }
        Shoot();
    }

    IEnumerator ChargeShot(float elapsedTime) {
        effect = Instantiate(chargeEffect, transform.position, transform.rotation, transform);
        while (elapsedTime < forcedShotTime) {
            elapsedTime += Time.deltaTime;
            shotSpeed += chargeRate * Time.deltaTime;

            if (inputDevice.GetControl(shootButton).WasReleased) {

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
        shootTimer = null;
        var ball = ballCarrier.ball;
        Debug.Assert(ball != null);
        var shotDirection = ball.transform.position - transform.position;
        var ballRigidBody = ball.EnsureComponent<Rigidbody2D>();
        ballRigidBody.velocity = shotDirection.normalized * shotSpeed;
        stateManager.CurrentStateHasFinished();
    }

    void StopChargeShot() {
        circularTimer?.StopTimer();
        if (shootTimer != null) {
            StopCoroutine(shootTimer);
            shootTimer = null;
        }
        if (effect != null) {
            Destroy(effect);
        }
    }

    void Update() {
        if (inputDevice == null) {
            inputDevice = playerMovement.GetInputDevice();
        }
    }


}
