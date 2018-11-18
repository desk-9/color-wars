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
        stateManager.CallOnStateEnter(
            State.Stun, () => StartCoroutine(StunEffectRoutine()));
        stateManager.CallOnStateExit(State.Stun, () => stopEffect = true);
    }

    private IEnumerator StunEffectRoutine()
    {
        SpriteRenderer renderer = this.EnsureComponent<SpriteRenderer>();
        Color baseColor = renderer.color;
        TeamManager team = GetComponent<Player>()?.team;
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
