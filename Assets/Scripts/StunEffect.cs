using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class StunEffect : MonoBehaviour {
    public float flashInterval = 0.1f;

    bool stopEffect = false;
    void Start() {
        var stateManager = this.EnsureComponent<PlayerStateManager>();
        stateManager.CallOnStateEnter(
            State.Stun, () => StartCoroutine(StunEffectRoutine()));
        stateManager.CallOnStateExit(State.Stun, () => stopEffect = true);
    }



    IEnumerator StunEffectRoutine() {
        var renderer = this.EnsureComponent<SpriteRenderer>();
        var baseColor = renderer.color;
        var team = GetComponent<Player>()?.team;
        var shiftedColor = Color.white;

        while (!stopEffect) {
            renderer.color = shiftedColor;
            yield return new WaitForSeconds(flashInterval);
            renderer.color = baseColor;
            yield return new WaitForSeconds(flashInterval);
        }
        stopEffect = false;
    }
}
