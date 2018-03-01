using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IC = InControl;

public class BallCarrier : MonoBehaviour {

    // Controls
    public IC.InputControlType dropBallButton = IC.InputControlType.Action3; // X

    // Tweakables
    public float stunTime = 1.0f;
    public float ballDistance = 1.0f;

    // Non-public fields
    protected Ball ball = null;
    PlayerMovement playerMovement;
    IC.InputDevice input;

    public bool IsCarryingBall {
        get {return ball != null;}
    }

    void Start() {
	playerMovement = GetComponent<PlayerMovement>();
        input = playerMovement.GetInputDevice();
    }

    // This function is called when the BallCarrier initially gains possession
    // of the ball
    public virtual void CarryBall(Ball ballIn) {
	Debug.Log("Carrying ball!");
        ball = ballIn;
	playerMovement.StopAllMovement();
    }

    public virtual void DropBall() {
	if (ball != null) {
	    Debug.Log("Dropping ball!");

	    // Restart player motion
            playerMovement.StartPlayerMovement();

            // Stop the ball
            var ballRigidbody = ball.GetComponent<Rigidbody2D>();
            ballRigidbody.velocity = Vector3.zero;

	    // Reset references
	    ball.owner = null;
	    ball = null;
	}
    }

    public virtual void Update() {
	if (input == null) {
            input = playerMovement.GetInputDevice();
            return;
        }

	UpdateBallPosition();
	if (input.GetControl(dropBallButton).WasPressed) {
	    DropBall();
	}

    }

    public virtual void UpdateBallPosition() {
        if (ball!= null) {
            var targetPosition = transform.position;
            var offsetVector = -transform.forward * ballDistance;
            ball.transform.position = targetPosition + offsetVector;
        }
    }

}
