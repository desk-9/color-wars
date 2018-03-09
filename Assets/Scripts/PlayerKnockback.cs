using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class PlayerKnockback : MonoBehaviour {
    public float knockbackTime = 5f;

    Coroutine knockbacking;
    new Rigidbody2D rigidbody;

    void Awake() {
        rigidbody = this.EnsureComponent<Rigidbody2D>();
    }

    public void StartKnockback(Vector2 direction, float speed) {
        Debug.Log("Start knockback");
        knockbacking = StartCoroutine(Knockback(direction, speed));
    }

    IEnumerator Knockback(Vector2 direction, float speed, float? length = null) {
        if (length == null) {
            length = knockbackTime;
        }
        Debug.LogFormat("{0}, {1}", direction, speed);
        var endTime = Time.time + length;
        while (Time.time < endTime) {
            rigidbody.velocity = direction * speed;
            yield return null;
        }
        this.GetComponent<PlayerStateManager>()?.CurrentStateHasFinished();
    }

    public void StopKnockbacking() {
        if (knockbacking != null) {
            StopCoroutine(knockbacking);
            knockbacking = null;
        }
    }
}
