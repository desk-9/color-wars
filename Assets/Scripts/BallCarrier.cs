using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

using IC = InControl;

public class BallCarrier : MonoBehaviour {

    public float ballOffsetFromCenter = .5f;
    public float coolDownTime = .1f;

    public Ball ball { private set; get;}
    PlayerMovement playerMovement;
    IC.InputDevice input;
    PlayerStateManager stateManager;
    Coroutine carryBallCoroutine;
    bool isCoolingDown = false;

    public bool IsCarryingBall() {
        return ball != null;
    }

    void Start() {
        playerMovement = GetComponent<PlayerMovement>();
        input = playerMovement?.GetInputDevice();
        stateManager = GetComponent<PlayerStateManager>();
        if (playerMovement != null && stateManager != null) {
            stateManager.SignUpForStateAlert(
                State.Posession,
                (bool starting) => {
                    if (starting) {
                        playerMovement.FreezePlayer();
                    } else {
                        playerMovement.UnFreezePlayer();
                    }
                });
        }
    }

    // This function is called when the BallCarrier initially gains possession
    // of the ball
    void StartCarryingBall(Ball ball) {
        carryBallCoroutine = StartCoroutine(CarryBall(ball));
    }

    IEnumerator CarryBall(Ball ball) {
        Debug.Log("Carrying ball! Owner: " + gameObject.name);
        GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        this.ball = ball;
        ball.owner = this;
        
        var ballCollider = ball.gameObject.GetComponent<CircleCollider2D>();
        if (ballCollider != null) {
            ballCollider.enabled = false;
        }

        while (true) {
            playerMovement?.RotatePlayer();
            PlaceBallAtNose();
            yield return null;
        }
    }

    IEnumerator CoolDownTimer() {
        isCoolingDown = true;
        yield return new WaitForSeconds(coolDownTime);
        isCoolingDown = false;
    }

    public void DropBall() {
        if (ball != null) {
            Debug.Log("Dropping ball! Owner: " + gameObject.name);

            StopCoroutine(carryBallCoroutine);
            carryBallCoroutine = null;

            var ballCollider = ball.gameObject.GetComponent<CircleCollider2D>();
            if (ballCollider != null) {
                ballCollider.enabled = true;
            }

            // Reset references
            ball.owner = null;
            ball = null;
            StartCoroutine(CoolDownTimer());
        }
    }

    void PlaceBallAtNose() {
        if (ball != null) {
            ball.transform.position = transform.position +
                (transform.right * ballOffsetFromCenter);
        }

    }

    public virtual void Update() {
        if (input == null) {
            input = playerMovement?.GetInputDevice();
            return;
        }
    }

    public void OnCollisionEnter2D(Collision2D collision) {
        var ball = collision.gameObject.GetComponent<Ball>();
        if (ball == null || ball.HasOwner() || isCoolingDown) {
            return;
        }
        if (stateManager != null) {
            stateManager.AttemptPossession(() => StartCarryingBall(ball), DropBall);
        } else {
            StartCoroutine(CoroutineUtility.RunThenCallback(CarryBall(ball), DropBall));
        }
    }


}
