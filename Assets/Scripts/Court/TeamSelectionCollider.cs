using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class TeamSelectionCollider : MonoBehaviour
{

    public int teamNumber;
    private int maxOnTeam = 2;
    public TeamManager team { get; set; }
    public bool mustDashToSwitch = true;
    private Text countText;
    private GameObject impactEffectPrefab;

    private void Start()
    {
        countText = GetComponentInChildren<Text>();
        if (teamNumber < GameManager.instance.teams.Count)
        {
            team = GameManager.instance.teams[teamNumber];
        }
        this.FrameDelayCall(() =>
        {
            impactEffectPrefab = team.resources.selectTeamImpactEffectPrefab;
        }, 2);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null && team != null && player.team != team)
        {
            PlayerStateManager stateManager = player.GetComponent<PlayerStateManager>();
            if (stateManager != null)
            {
                if (mustDashToSwitch && !stateManager.IsInState(DEPRECATED_State.Dash))
                {
                    // Only switch if dashing
                    return;
                }
            }
            if (player.team != team && team.members.Count < maxOnTeam)
            {
                player.SetTeam(team);
                AudioManager.instance.Ching.Play();
                SpawnHitEffect(player.transform.position);
            }
        }
    }

    private void SpawnHitEffect(Vector3 playerPosition)
    {
        float spawnAngle = Vector3.SignedAngle(
            playerPosition - transform.position, Vector3.up, Vector3.forward);
        // These magic-ish numbers depend heavily on the fact that the impact
        // effect is a hemisphere which rotates to show where the player
        // impacted the team selection collider
        GameObject impactEffect = Instantiate(impactEffectPrefab, playerPosition,
                                       Quaternion.Euler(spawnAngle - 90.0f, 90.0f, -90.0f));
        ParticleSystem ps = impactEffect.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }
    }

    private int lastCount = 0;

    private void FixedUpdate()
    {
        if (team != null && team.members.Count != lastCount)
        {
            if (countText != null)
            {
                countText.text = string.Format("{0}/{1}", team.members.Count, 2);
            }
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (team.members.Count >= maxOnTeam)
            {
                this.TimeDelayCall(() =>
                {
                    AudioManager.instance.GoalSwitch.Play();
                    renderer.color = 0.85f * team.color.color;
                }, 0.3f);
            }
            else
            {
                renderer.color = team.color;
            }
            lastCount = team.members.Count;
        }
    }
}
