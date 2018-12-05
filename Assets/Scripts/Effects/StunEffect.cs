using System.Collections;
using UnityEngine;
using UtilityExtensions;

using EM = EventsManager;
public class StunEffect : MonoBehaviour
{
    public float flashInterval = 0.1f;
    private bool stopEffect = false;

    private void Start()
    {
        // PlayerStateManager stateManager = this.EnsureComponent<PlayerStateManager>();
        EM.onPlayerStunned += (EM.onPlayerStunnedArgs args) => {
            Player player = args.player;
            if (player.gameObject == this.gameObject) {
                StartCoroutine(StunEffectRoutine());
            }
        };
        EM.onPlayerUnstunned += (EM.onPlayerUnstunnedArgs args) => {
            Player player = args.player;
            if (player.gameObject == this.gameObject) {
                stopEffect = true;
            }
        };
    }

    // private void HandleNewPlayerState(State oldState, State newState)
    // {
    //     if (newState == State.Stun)
    //     {
    //         StartCoroutine(StunEffectRoutine());
    //     }
    //     if (oldState == State.Stun)
    //     {
    //         stopEffect = true;
    //     }
    // }

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
