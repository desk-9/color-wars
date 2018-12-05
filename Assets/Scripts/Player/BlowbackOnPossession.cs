using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using EM = EventsManager;
public class BlowbackOnPossession : MonoBehaviour
{
    public GameObject blowbackEffectPrefab;
    public float blowbackRadius = 3f;
    public float blowbackForce = 5f;
    public float blowbackStunTime = 0.1f;

    public void Start() {
        EM.onStartedCarryingBall += BlowBackEnemyPlayers;
    }

    private void BlowBackEnemyPlayers(EM.onStartedCarryingBallArgs args)
    {
        BallCarrier player = args.ballCarrier;
        Ball ball = args.ball;

        // TeamManager enemyTeam = GameManager.instance.teams.Find(
        //     (teamManager) => teamManager != player.team);
        // Debug.Assert(enemyTeam != null);
        if (player.team == null)
        {
            return;
        }
        this.DoBlowbackParticleEffect(player.team.color);

        List<Player> enemyPlayers = GameManager.instance.players.Except(player.team.members).ToList();

        foreach (Player enemyPlayer in enemyPlayers)
        {
            Vector3 blowbackVector = enemyPlayer.transform.position - transform.position;
            if (blowbackVector.magnitude < blowbackRadius)
            {
                PlayerStun otherStun = enemyPlayer.GetComponent<PlayerStun>();
                if (otherStun != null)
                {
                    // TODO: fix -- make sure that events etc are aok
                    otherStun.StartStun(blowbackVector.normalized * blowbackForce,
                                        blowbackStunTime);
                }
            }
        }
    }

    private void DoBlowbackParticleEffect(Color teamColor) {
        GameObject effect = Instantiate(blowbackEffectPrefab, transform.position, transform.rotation);
        ParticleSystem ps = effect.GetComponent<ParticleSystem>();
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;

        col.enabled = true;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(teamColor, 0.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f,  0.0f),
                new GradientAlphaKey(0.25f, 0.75f),
                new GradientAlphaKey(0.0f,  1.0f)
            }
            );
        col.color = grad;

        Destroy(effect, 1.0f);
    }

}
