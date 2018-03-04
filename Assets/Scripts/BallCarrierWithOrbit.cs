using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallCarrierWithOrbit : BallCarrier {

    public float orbitSpeed = 4.0f;

    public override void CarryBall(Ball ballIn) {
	base.CarryBall(ballIn);
	var ballCollider = ball.gameObject.GetComponent<CircleCollider2D>();
	if (ballCollider != null) {
	    ballCollider.enabled = false;
	}
    }

    public override void DropBall() {
        if (ball != null) {
            var ballCollider = ball.gameObject.GetComponent<CircleCollider2D>();
            if (ballCollider != null) {
                ballCollider.enabled = true;
            }
            base.DropBall();
        }
    }

    public override void UpdateBallPosition () {
	if (ball != null) {
	    var target = transform.position;
            // Based on the OrbitTarget demo, posted by Austin Yarger
            // See https://piazza.com/class/jbcr9wzhymw6hm?cid=214
	    Vector3 newPosition = target + new Vector3(Mathf.Cos(Time.time * orbitSpeed), Mathf.Sin (Time.time * orbitSpeed), 0) * ballDistance;
	    ball.transform.position = newPosition;
	}
    }

}
