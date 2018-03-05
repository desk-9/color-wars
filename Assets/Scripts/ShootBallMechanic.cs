using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using IC = InControl;

public class ShootBallMechanic : MonoBehaviour {

    public float shotSpeed;
    public float forcedShotTime;
    public IC.InputControlType shootButton = IC.InputControlType.Action1;

    PlayerMovement playerMovement;
    IC.InputDevice inputDevice;
    Coroutine shootTimer;

    public void WatchForShoot(Ball ball, Callback shotBallCallback) {
	shootTimer = StartCoroutine(ShootTimer(ball, shotBallCallback));
    }

    IEnumerator ShootTimer(Ball ball, Callback shotBallCallback) {
	float elapsedTime = 0f;
	while (elapsedTime < forcedShotTime) {
	    playerMovement.RotatePlayer();

	    if (inputDevice != null) {
		if (inputDevice.GetControl(shootButton).WasPressed){
		    Shoot(ball, shotBallCallback);
		    yield break;
		}
	    }
		
	    elapsedTime += Time.deltaTime;
	    yield return null;
	}
	Shoot(ball, shotBallCallback);
    }

    void Shoot(Ball ball, Callback shotBallCallback) {
	shootTimer = null;
	shotBallCallback();

	Debug.Log("Shooting ball");
	
	var shotDirection = ball.transform.position - transform.position;
	var ballRigidBody = ball.GetComponent<Rigidbody2D>();
	ballRigidBody.velocity = shotDirection.normalized * shotSpeed;
    }

    void Update() {
	if (inputDevice == null) {
	    inputDevice = playerMovement.GetInputDevice();
	}
    }

    void Start() {
	playerMovement = GetComponent<PlayerMovement>();
    }
}
