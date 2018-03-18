using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using IC = InControl;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerParryBehavior : MonoBehaviour {
    public CircleCollider2D    parryField;
    public IC.InputControlType parryButton = IC.InputControlType.Action3;
    public GameObject          parryEffect;
    public float               cooldown    = 1.0f;
    public float               knockback   = 1.0f;
    public List<LayerMask>     whitelist   = new List<LayerMask>();

    PlayerMovement   playerMovement;
    Coroutine        parryCoroutine;
    GameObject       effect;

    void Awake() {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update() {
        var input = playerMovement.GetInputDevice();
        if (input != null && parryCoroutine == null && input.GetControl(parryButton).WasPressed) {
            parryCoroutine = StartCoroutine(Parry());
        }
    }

    IEnumerator Parry() {
        effect = Instantiate(parryEffect, transform.position, Quaternion.identity);

        var objectsInRange = new List<GameObject>();

        // Compile list of objects in range using whitelist.
        foreach (var layer in whitelist) {
            var numInRange = 10;
            var colliders  = new Collider2D[numInRange];
            var filter     = new ContactFilter2D();

            filter.SetLayerMask(layer);

            numInRange = parryField.OverlapCollider(filter, colliders);

            for (var i = 0; i < numInRange; ++i) {
                objectsInRange.Add(colliders[i].gameObject);
            }
        }

        // Apply knockback for all objects in range.
        foreach (var obj in objectsInRange) {
            var rb = obj.GetComponent<Rigidbody2D>();

            if (rb != null) {
                var mag = rb.velocity.magnitude;
                var dir = (obj.transform.position - transform.position).normalized;

                rb.velocity = dir * (mag + knockback);
            }
        }

        yield return new WaitForSeconds(cooldown);

        Destroy(effect, cooldown + 1.0f);
        parryCoroutine = null;
        effect = null;

        yield break;
    }
}
