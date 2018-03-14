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
            rigidbody?.AddForce(knockbackVelocity.Value * rigidbody.mass,
                                ForceMode2D.Impulse);
        }
        stunned = StartCoroutine(Stun(length));
    }

    IEnumerator Stun(float? length = null) {
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
