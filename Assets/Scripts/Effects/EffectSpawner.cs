using UnityEngine;
using UtilityExtensions;

public enum EffectType
{
    DashCharge, Unset
}

public class EffectSpawner : MonoBehaviour
{
    public GameObject effectPrefab;
    public State triggerState;
    public bool destroyEffectOnExit = true;
    public bool parentEffectToPlayer = true;
    public float destroyWait = 0.0f;
    public EffectType effectType = EffectType.Unset;
    private GameObject currentEffect;
    private PlayerStateManager stateManager;

    private void Start()
    {
        stateManager = this.EnsureComponent<PlayerStateManager>();
        stateManager.OnStateChange += HandleNewPlayerState;
    }

    private void HandleNewPlayerState(State oldState, State newState)
    {
        if (newState == triggerState)
        {
            StateStart();
        }
        if (oldState == triggerState)
        {
            StateEnd();
        }
    }

    private void StateStart()
    {
        if (parentEffectToPlayer)
        {
            currentEffect = Instantiate(
                effectPrefab, transform.position, transform.rotation, transform);
        }
        else
        {
            currentEffect = Instantiate(
                effectPrefab, transform.position, Quaternion.identity);
        }
    }

    private void StateEnd()
    {
        if (destroyEffectOnExit && currentEffect != null)
        {
            Destroy(currentEffect, destroyWait);
        }
    }
}
