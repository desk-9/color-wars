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
        if (teamNumber < GameManager.Instance.Teams.Count)
        {
            team = GameManager.Instance.Teams[teamNumber];
        }
        this.FrameDelayCall(() =>
        {
            impactEffectPrefab = team.resources.selectTeamImpactEffectPrefab;
        }, 2);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null && team != null && player.Team != team)
        {
            PlayerStateManager stateManager = player.GetComponent<PlayerStateManager>();
            if (stateManager != null)
            {
                // The player could have already transitioned to a different state
                // after running into the team selection collider, so we also need to check
                // if the previous state was dash and if the player changed states in this frame.
                bool playerHitWhileDashing = stateManager.CurrentState == State.Dash ||
                    (stateManager.PreviousState == State.Dash &&
                    (Time.time - stateManager.TimeOfLastStateChange) < Time.deltaTime);
                if (mustDashToSwitch && !playerHitWhileDashing)
                {
                    // Only switch if dashing
                    return;
                }
            }
            // Networking info: local team selection doesn't apply to non-local
            // players. Instead we wait for them to send us their team state.
            if (player.Team != team && team.teamMembers.Count < maxOnTeam &&
                player.photonView.IsMine)
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
        // TODO refactor to use the TeamsChanged event
        if (team != null && team.teamMembers.Count != lastCount)
        {
            if (countText != null)
            {
                countText.text = string.Format("{0}/{1}", team.teamMembers.Count, 2);
            }
            SpriteRenderer renderer = GetComponent<SpriteRenderer>();
            if (team.teamMembers.Count >= maxOnTeam)
            {
                this.TimeDelayCall(() =>
                {
                    AudioManager.instance.GoalSwitch.Play();
                    renderer.color = 0.85f * team.TeamColor.color;
                }, 0.3f);
            }
            else
            {
                renderer.color = team.TeamColor;
            }
            lastCount = team.teamMembers.Count;
        }
    }
}
