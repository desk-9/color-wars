using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallCatcher : MonoBehaviour {

	void OnTriggerEnter2D(Collider2D collider) {
        var ball = collider.gameObject.GetComponent<Ball>();
        if (ball != null) {
            ball.ResetBall();
        }
    }
}
