using UnityEngine;

public class BallCatcher : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collider)
    {
        Ball ball = collider.gameObject.GetComponent<Ball>();
        if (ball != null)
        {
            ball.ResetBall();
        }
    }
}
