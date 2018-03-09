using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class PlayerStun : MonoBehaviour {
    public float stunTime = 5f;

    Coroutine stunned;

    public void StartStun(float? length = null) {
        stunned = StartCoroutine(Stun(length));
    }

    IEnumerator Stun(float? length = null) {
        if (length == null) {
            length = stunTime;
        }
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
