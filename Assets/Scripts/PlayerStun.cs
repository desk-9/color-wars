using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class PlayerStun : MonoBehaviour {
    public float stunTime = 5f;

    Coroutine stunned;
    Rigidbody2D rb2d;

    void Start() {
        rb2d = GetComponent<Rigidbody2D>();
    }

    public void StartStun(Vector2 knockback, float? length = null) {
        stunned = StartCoroutine(Stun(knockback, length));
    }

    IEnumerator Stun(Vector2 knockback, float? length = null) {
        if (length == null) {
            length = stunTime;
        }

        rb2d.AddForce(knockback);

        var endTime = Time.time + length;
        while (Time.time < endTime) {
            yield return null;
        }
        this.GetComponent<PlayerStateManager>()?.CurrentStateHasFinished();
    }

    public void StopStunned() {
        if (stunned != null) {
            StopCoroutine(stunned);
            stunned = null;
        }
    }
}
