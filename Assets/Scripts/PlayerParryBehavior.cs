using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IC = InControl;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerParryBehavior : MonoBehaviour {
    public CircleCollider2D    parryField;
    public IC.InputControlType parryButton = IC.InputControlType.Action3;
    public float               cooldown    = 1.0f;
    public float               knockback   = 1.0f;

    PlayerMovement   playerMovement;
    IC.InputDevice   input;
    Coroutine        parryCoroutine;

    void Awake() {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update() {
        if (input == null) {
            input = playerMovement.GetInputDevice();
            return;
        }

        if (parryCoroutine == null && input.GetControl(parryButton).WasPressed) {
            parryCoroutine = StartCoroutine(Parry());
        }
    }

    IEnumerator Parry() {
        var colliders = new Collider2D[10];
        var filter    = new ContactFilter2D();

        int numInRange = parryField.OverlapCollider(filter, colliders);

        for (var i = 0; i < numInRange; ++i) {
            var coll = colliders[i];

            var obj = coll?.gameObject;
            var rb  = obj?.GetComponent<Rigidbody2D>();

            if (rb != null) {
                var mag = rb.velocity.magnitude;
                var dir = (obj.transform.position - transform.position).normalized;

                rb.velocity = dir * (mag + knockback);
            }
        }

        yield return new WaitForSeconds(cooldown);

        parryCoroutine = null;

        yield break;
    }
}
