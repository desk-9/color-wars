using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

delegate Vector2 VectorCallback();

public class BallSpriteRotation : MonoBehaviour {

    new SpriteRenderer renderer;
    Coroutine rotationCoroutine;
    VectorCallback GetKeepToVector;

    // Use this for initialization
    void Start () {
        renderer = this.EnsureComponent<SpriteRenderer>();
        GetKeepToVector = () => transform.right;
        GameModel.instance.nc.CallOnMessage(Message.BallIsUnpossessed, BecameUnpossessed);
        GameModel.instance.nc.CallOnMessageWithSender(Message.BallIsPossessed,
                                                      BecamePossessed);
    }

    void BecamePossessed(object ballObj) {
        var ball = ballObj as Ball;
        if (ball == null) {
            return;
        }

        var angleDifference = Vector2.SignedAngle(ball.owner.transform.right, transform.right);
        GetKeepToVector = () => Utility.RotateVector(ball.owner.transform.right, angleDifference);
    }

    void BecameUnpossessed() {
        var right = transform.right;
        GetKeepToVector = () => right;
    }

    // Update is called once per frame
    void Update () {
        transform.right = GetKeepToVector();
    }
}
