﻿using System.Collections;
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
    public float chargeEffectOffset = 1.5f;

    PlayerMovement playerMovement;
    PlayerStateManager stateManager;
    Coroutine shootTimer;
    Coroutine chargeShot;
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
        circularTimer = Instantiate(
            circularTimerPrefab, transform).GetComponent<CircularTimer>();
        circularTimer.transform.localScale = circleTimerScale;

        var ball = GameObject.FindObjectOfType<Ball>();
        maxShotSpeed = baseShotSpeed + Mathf.Pow((1 + forcedShotTime * chargeRate), shotPower);
        this.FrameDelayCall(() => team = this.GetComponent<Player>()?.team, 2);
    }

    void StartTimer() {
        shootTimer = StartCoroutine(ShootTimer());
    }

    void ShootPressed() {
        if (stateManager.IsInState(State.Posession) && shootTimer != null) {
            StopCoroutine(shootTimer);
            shootTimer = null;
            chargeShot = StartCoroutine(ChargeShot());
        }
    }

    void ShootReleased() {
        if (chargeShot != null) {
            StopCoroutine(chargeShot);
            shotSpeed = baseShotSpeed + Mathf.Pow(shotSpeed, shotPower);
            Shoot();
        }
    }

    IEnumerator ShootTimer() {
        this.FrameDelayCall(() => circularTimer?.StartTimer(forcedShotTime, delegate{}), 2);

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
        effect = Instantiate(team.resources.shootChargeEffectPrefab,
                             transform.position + transform.right * chargeEffectOffset,
                             transform.rotation, transform);

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
        chargeShot = null;
        var ball = ballCarrier.ball;
        var shotDirection = ball.transform.position - transform.position;

        var ballRigidBody = ball.EnsureComponent<Rigidbody2D>();
        if (shotSpeed / maxShotSpeed >= 0.45f) {
            Utility.TutEvent("ShootCharge", this);
        }
        ballRigidBody.velocity = shotDirection.normalized * shotSpeed;
        stateManager.CurrentStateHasFinished();
    }

    void StopChargeShot() {
        circularTimer?.StopTimer();
        if (shootTimer != null) {
            StopCoroutine(shootTimer);
            shootTimer = null;
        }
        if (chargeShot != null) {
            StopCoroutine(chargeShot);
            chargeShot = null;
        }

        if (effect != null) {
            Destroy(effect);
        }
        playerMovement.freezeRotation = false;
    }

}
