using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class PlayerStun : MonoBehaviour {
    public float stunTime = 5f;

    Coroutine stunned;

    public void StartStun(Vector2? knockbackVelocity = null, float? length = null) {
        if (knockbackVelocity != null) {
            var rigidbody = GetComponent<Rigidbody2D>();
            if (rigidbody != null) {
                this.FrameDelayCall(() => rigidbody.velocity = knockbackVelocity.Value);
            }
        }
        stunned = StartCoroutine(Stun(length));
    }

    IEnumerator Stun(float? length = null) {
        if (length == null) {
            length = stunTime;
        }
        var endTime = Time.time + length.Value;
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
