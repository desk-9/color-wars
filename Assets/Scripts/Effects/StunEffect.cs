using System.Collections;
using UnityEngine;
using UtilityExtensions;

// TODO dkonik: Consolidate with PlayerStun...maybe?
public class StunEffect : MonoBehaviour
{
    [SerializeField]
    private float stunFlashInterval = .1f;

    private bool stopEffect = false;


    private void Start()
    {
        PlayerStateManager stateManager = this.EnsureComponent<PlayerStateManager>();
        stateManager.OnStateChange += HandleNewPlayerState;
    }

    private void HandleNewPlayerState(State oldState, State newState)
    {
        if (newState == State.Stun)
        {
            StartCoroutine(StunEffectRoutine());
        }
        if (oldState == State.Stun)
        {
            stopEffect = true;
        }
    }

    private IEnumerator StunEffectRoutine()
    {
        SpriteRenderer renderer = this.EnsureComponent<SpriteRenderer>();
        Color baseColor = renderer.color;
        Color shiftedColor = Color.white;

        while (!stopEffect)
        {
            renderer.color = shiftedColor;
            yield return new WaitForSeconds(stunFlashInterval);
            renderer.color = baseColor;
            yield return new WaitForSeconds(stunFlashInterval);
        }
        stopEffect = false;
    }
}
