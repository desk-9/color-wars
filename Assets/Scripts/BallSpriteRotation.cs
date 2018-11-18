using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

delegate Vector2 VectorCallback();

public class BallSpriteRotation : MonoBehaviour {

    Coroutine rotationCoroutine;
    VectorCallback GetKeepToVector;

    // Use this for initialization
    void Start () {
        GetKeepToVector = () => transform.right;
        GameModel.instance.notificationCenter.CallOnMessage(Message.BallIsUnpossessed, BecameUnpossessed);
        GameModel.instance.notificationCenter.CallOnMessageWithSender(Message.BallIsPossessed,
                                                      BecamePossessed);
    }

    void BecamePossessed(object ballObj) {
        var ball = ballObj as Ball;
        if (ball == null) {
            return;
        }

        var angleDifference = Vector2.SignedAngle(ball.Owner.transform.right, transform.right);
        GetKeepToVector = () => Utility.RotateVector(ball.Owner.transform.right, angleDifference);
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
