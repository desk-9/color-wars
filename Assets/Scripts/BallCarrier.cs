using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IC = InControl;

public class BallCarrier : MonoBehaviour {

    public float stunTime = 1.0f;
    public float ballDistance = 1.0f;
    public float ballOffsetFromCenter = .5f;

    protected Ball ball = null;
    PlayerMovement playerMovement;
    IC.InputDevice input;
    ShootBall shootBall;

    public bool IsCarryingBall {
        get {return ball != null;}
    }

    void Start() {
	playerMovement = GetComponent<PlayerMovement>();
        input = playerMovement.GetInputDevice();
	shootBall = GetComponent<ShootBall>();
    }

    // This function is called when the BallCarrier initially gains possession
    // of the ball
    public virtual void CarryBall(Ball ballIn) {
	Debug.Log("Carrying ball! Owner: " + gameObject.name);
        ball = ballIn;
	playerMovement.StopAllMovement();
	
	var ballCollider = ball.gameObject.GetComponent<CircleCollider2D>();
	if (ballCollider != null) {
	    ballCollider.enabled = false;
	}

	PlaceBallAtNose();
	shootBall.WatchForShoot(ballIn, DropBall);
    }

    public virtual void DropBall() {
	if (ball != null) {
	    Debug.Log("Dropping ball! Owner: " + gameObject.name);

	    var ballCollider = ball.gameObject.GetComponent<CircleCollider2D>();
            if (ballCollider != null) {
                ballCollider.enabled = true;
            }

	    // Restart player motion
            playerMovement.StartPlayerMovement();

	    // Reset references
	    ball.RemoveOwner();
	    ball = null;
	}
    }

    void PlaceBallAtNose() {
	if (ball != null) {
	    ball.transform.position = transform.position +
		(transform.right * ballOffsetFromCenter);
	}

    }

    public virtual void Update() {
	PlaceBallAtNose();
        
	if (input == null) {
            input = playerMovement.GetInputDevice();
            return;
        }
    }

}
