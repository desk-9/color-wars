using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using IC = InControl;

public class ShootBallMechanic : MonoBehaviour {

    public IC.InputControlType shootButton = IC.InputControlType.Action1;
    public float forcedShotTime;
    public float baseShotSpeed = 1.0f;
    public float chargeRate    = 1.0f;
    public GameObject          chargeEffect;

    PlayerMovement     playerMovement;
    PlayerStateManager stateManager;
    Rigidbody2D        rb;
    IC.InputDevice     inputDevice;
    Coroutine          shootTimer;
    Coroutine          chargeShot;
    BallCarrier        ballCarrier;
    GameObject         effect;
    bool canShoot = false;

    float shotSpeed = 1.0f;
    float elapsedTime = 0.0f;

    IEnumerator ShootTimer() {
        Debug.Log("ShootTimer!");
        elapsedTime = 0.0f;
        while (elapsedTime < forcedShotTime) {
            elapsedTime += Time.deltaTime;
            if (inputDevice.GetControl(shootButton).WasPressed) {
                StartChargeShot();
            }
            yield return null;
        }

        shootTimer = null;
        Shoot();
        yield break;
    }


    void StartChargeShot() {
        Debug.Log("StartChargeShot!");
        chargeShot = StartCoroutine(ChargeShot());
        effect = Instantiate(chargeEffect, transform.position, Quaternion.identity, transform);
    }
    IEnumerator ChargeShot() {
        Debug.Log("ChargeShot!");
        while (elapsedTime < forcedShotTime) {
            shotSpeed += chargeRate * Time.deltaTime;
            if (inputDevice.GetControl(shootButton).WasReleased) {
                Debug.Log("Trigger released => Shoot!");
                chargeShot = null;
                Shoot();
                yield break;
            }
            yield return null;
        }
    }

    void Shoot() {
        if (!canShoot) {
            Debug.LogWarning("Can't shoot!");
            return;
        }
        var ball = ballCarrier.ball;
        Debug.Assert(ball != null);
        var shotDirection = ball.transform.position - transform.position;
        var ballRigidBody = ball.EnsureComponent<Rigidbody2D>();
        ballRigidBody.velocity = shotDirection.normalized * shotSpeed;
        stateManager.CurrentStateHasFinished();
        StopChargeShot();
    }

    void StopChargeShot() {
        Debug.Log("StopChargeShot!");
        // Release player from position lock.
        rb.constraints = RigidbodyConstraints2D.None;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        if (shootTimer != null) {
            StopCoroutine(shootTimer);
            shootTimer = null;
        }
        if (effect != null) {
            Destroy(effect);
        }
        if (chargeShot != null) {
            StopCoroutine(chargeShot);
            chargeShot = null;
        }
    }

    void Update() {
        if (inputDevice == null) {
            inputDevice = playerMovement.GetInputDevice();
            return;
        }
    }

    void Start() {
        playerMovement = this.EnsureComponent<PlayerMovement>();
        ballCarrier  = this.EnsureComponent<BallCarrier>();
        rb           =  this.EnsureComponent<Rigidbody2D>();
        stateManager =  this.EnsureComponent<PlayerStateManager>();
        stateManager.CallOnStateEnter(
            State.Posession, () => shootTimer = StartCoroutine(ShootTimer()));
        stateManager.CallOnStateExit(
            State.Posession,
            () => {
                if (shootTimer != null) {
                    StopCoroutine(shootTimer);
                    shootTimer = null;
                }
            });
    }
}
