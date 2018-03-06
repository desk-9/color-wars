using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviour {	
    // A BallCarrier is allowed to "possess" or "carry" the ball
    // The `owner` property stores a reference to the current owner.

    Vector2 start_location;
    BallCarrier owner = null;

    public void RemoveOwner() {
        owner = null;
    }

    public void SetOwner(BallCarrier ballCarrier){
	owner = ballCarrier;
    }

    public bool HasOwner() {
	return owner != null;
    }

    void Start() {
        start_location = transform.position;
    }

    public void ResetBall() {
        transform.position = start_location;
        this.EnsureComponent<Rigidbody2D>().velocity = Vector2.zero;
    }
}
