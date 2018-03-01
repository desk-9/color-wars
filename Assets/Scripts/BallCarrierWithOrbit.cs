using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallCarrierWithOrbit : BallCarrier {

    public float rate = 4.0f;

    public override void CarryBall(Ball ballIn) {
	base.CarryBall(ballIn);
	var ballCollider = ball.gameObject.GetComponent<CircleCollider2D>();
	if (ballCollider != null) {
	    ballCollider.enabled = false;
	}
    }

    public override void DropBall() {
	var ballCollider = ball.gameObject.GetComponent<CircleCollider2D>();
	if (ballCollider != null) {
	    ballCollider.enabled = true;
	}
	base.DropBall();
    }

    public override void UpdateBallPosition () {
	if (ball != null) {
	    var target = transform.position;
            // Based on the OrbitTarget demo, posted by Austin Yarger
            // See https://piazza.com/class/jbcr9wzhymw6hm?cid=214
	    Vector3 newPosition = target + new Vector3(Mathf.Cos(Time.time * rate), Mathf.Sin (Time.time * rate), 0) * ballDistance;
	    ball.transform.position = newPosition;
	}
    }

}
