using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class DummyMovement : MonoBehaviour, IPlayerMovement {

    PlayerStateManager stateManager;
    new Rigidbody2D rigidbody;
    Coroutine stayingStill;
    // Use this for initialization
    void Start () {
        rigidbody = GetComponent<Rigidbody2D>();
        stateManager = GetComponent<PlayerStateManager>();
        this.FrameDelayCall(() => {
                stateManager.AttemptNormalMovement(StartStayStill, EndStayStill);
            }, 1);
    }

    void StartStayStill() {
        stayingStill = StartCoroutine(StayStill());
    }

    void EndStayStill() {
        StopCoroutine(stayingStill);
        stayingStill = null;
    }

    IEnumerator StayStill() {
        while (true) {
            rigidbody.velocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }
    }

    public void RotatePlayer() {
        rigidbody.rotation = Mathf.Repeat(rigidbody.rotation + 1f, 360);
    }

    public void FreezePlayer() {
        rigidbody.isKinematic = true;
    }

    public void UnFreezePlayer() {
        rigidbody.isKinematic = false;
    }
}
