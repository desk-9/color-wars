using UnityEngine;
using UtilityExtensions;

public class EffectSpawner : MonoBehaviour {
    public GameObject effectPrefab;
    GameObject currentEffect;
    public State triggerState;
    public bool destroyEffectOnExit = true;
    public bool parentEffectToPlayer = true;
    PlayerStateManager stateManager;

    void Start() {
        stateManager = this.EnsureComponent<PlayerStateManager>();
        stateManager.SignUpForStateAlert(triggerState, EffectToggle);
    }

    void EffectToggle(bool effectStarting) {
        if (effectStarting) {
            if (parentEffectToPlayer) {
                currentEffect = Instantiate(effectPrefab, transform.position,
                                            Quaternion.identity, transform);
            } else {
                currentEffect = Instantiate(effectPrefab, transform.position,
                                            Quaternion.identity);
            }
        } else if (!effectStarting && destroyEffectOnExit) {
            Destroy(currentEffect);
        }
    }
}
