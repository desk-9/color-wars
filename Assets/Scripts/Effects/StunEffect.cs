using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class StunEffect : MonoBehaviour
{
    public float flashInterval = 0.1f;
    private bool stopEffect = false;

    private void Start()
    {
        PlayerStateManager stateManager = this.EnsureComponent<PlayerStateManager>();
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
            yield return new WaitForSeconds(flashInterval);
            renderer.color = baseColor;
            yield return new WaitForSeconds(flashInterval);
        }
        stopEffect = false;
    }
}
