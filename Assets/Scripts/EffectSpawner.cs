using UnityEngine;
using UtilityExtensions;

public class EffectSpawner : MonoBehaviour {
    public GameObject effectPrefab;
    public State triggerState;
    public bool destroyEffectOnExit = true;
    public bool parentEffectToPlayer = true;
    public float destroyWait = 0.0f;

    GameObject currentEffect;
    PlayerStateManager stateManager;

    void Start() {
        stateManager = this.EnsureComponent<PlayerStateManager>();
        stateManager.CallOnStateEnter(triggerState, StateStart);
        stateManager.CallOnStateExit(triggerState, StateEnd);
    }

    void StateStart() {
        if (parentEffectToPlayer) {
            currentEffect = Instantiate(
                effectPrefab, transform.position, transform.rotation, transform);
        } else {
            currentEffect = Instantiate(
                effectPrefab, transform.position, Quaternion.identity);
        }
    }

    void StateEnd() {
        if (destroyEffectOnExit && currentEffect != null) {
            Destroy(currentEffect, destroyWait);
        }
    }
}
