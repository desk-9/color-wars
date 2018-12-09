using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class DummyMovement : MonoBehaviour
{
    //private PlayerStateManager stateManager;
    //private new Rigidbody2D rigidbody;
    //private Coroutine stayingStill;

    //// Use this for initialization
    //private void Start()
    //{
    //    rigidbody = GetComponent<Rigidbody2D>();
    //    stateManager = GetComponent<PlayerStateManager>();
    //    this.FrameDelayCall(() =>
    //    {
    //        stateManager.AttemptNormalMovement(StartStayStill, EndStayStill);
    //    }, 1);
    //}

    //private void StartStayStill()
    //{
    //    stayingStill = StartCoroutine(StayStill());
    //}

    //private void EndStayStill()
    //{
    //    StopCoroutine(stayingStill);
    //    stayingStill = null;
    //}

    //private IEnumerator StayStill()
    //{
    //    while (true)
    //    {
    //        rigidbody.velocity = Vector2.zero;
    //        yield return new WaitForFixedUpdate();
    //    }
    //}
}
