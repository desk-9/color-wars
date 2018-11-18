﻿using UnityEngine;

internal delegate Vector2 VectorCallback();

public class BallSpriteRotation : MonoBehaviour
{
    private Coroutine rotationCoroutine;
    private VectorCallback GetKeepToVector;

    // Use this for initialization
    private void Start()
    {
        GetKeepToVector = () => transform.right;
        GameModel.instance.notificationCenter.CallOnMessage(Message.BallIsUnpossessed, BecameUnpossessed);
        GameModel.instance.notificationCenter.CallOnMessageWithSender(Message.BallIsPossessed,
                                                      BecamePossessed);
    }

    private void BecamePossessed(object ballObj)
    {
        Ball ball = ballObj as Ball;
        if (ball == null)
        {
            return;
        }

        float angleDifference = Vector2.SignedAngle(ball.Owner.transform.right, transform.right);
        GetKeepToVector = () => Utility.RotateVector(ball.Owner.transform.right, angleDifference);
    }

    private void BecameUnpossessed()
    {
        Vector3 right = transform.right;
        GetKeepToVector = () => right;
    }

    // Update is called once per frame
    private void Update()
    {
        transform.right = GetKeepToVector();
    }
}
