using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviour {

	
    // A BallCarrier is allowed to "possess" or "carry" the ball
    // The `owner` property stores a reference to the current owner.

    public float coolDownTime = .1f;

    Vector2 start_location;
    Coroutine coolDown;
    BallCarrier owner = null;
    BallCarrier lastOwner;

    public void RemoveOwner() {
        lastOwner = owner;
        owner = null;
        CoolDown();
    }

    void Start() {
        start_location = transform.position;
    }

    public void OnCollisionEnter2D(Collision2D collision) {
        // Can't switch owners if the ball is already owned by someone
        if (owner != null) {
            Debug.Log("Ball already has owner -- cannot switch owners");
            return;
        }
        // The assumption here is that a gameObject will have a BallCarrier component
        // iff the gameObject can own/carry the ball
        var player = collision.gameObject.GetComponent<BallCarrier>();
        if (coolDown != null && player == lastOwner) {
            return;
        }
	
        if (player != null) {
            owner = player;
            player.CarryBall(this);
        }
    }

    public void CoolDown() {
        coolDown = StartCoroutine(CoolDownCoroutine());
    }

    IEnumerator CoolDownCoroutine() {
        yield return new WaitForSeconds(coolDownTime);
        coolDown = null;
    }

    public void ResetBall() {
        transform.position = start_location;
        this.EnsureComponent<Rigidbody2D>().velocity = Vector2.zero;
    }
}
