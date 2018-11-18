using UnityEngine;
using UtilityExtensions;

public class Player : MonoBehaviour
{
    public delegate void OnTeamAssignedCallback(TeamManager team);
    public OnTeamAssignedCallback OnTeamAssigned = delegate { };

    public TeamManager team { get; private set; }
    public int playerNumber;
    // teamOverride sets which team a given player will join, overriding all
    // other methods of setting the team if it's non-negative
    public int teamOverride = -1;
    public Vector2 initialPosition;
    public float initialRotation;

    private bool isNormalPlayer = true;
    private new SpriteRenderer renderer;
    private PlayerStateManager stateManager;
    private Rigidbody2D rb2d;
    private new Collider2D collider;
    private GameObject explosionEffect;

    public void CallAsSoonAsTeamAssigned(OnTeamAssignedCallback callback)
    {
        if (team != null)
        {
            callback(team);
        }
        OnTeamAssigned += callback;
    }

    public void MakeInvisibleAfterGoal()
    {
        if (isNormalPlayer)
        {
            renderer.enabled = false;
            collider.enabled = false;

            stateManager.AttemptFrozenAfterGoal(
                GetComponent<PlayerMovement>().StartRotateOnly, delegate { }
            );
        }

        explosionEffect = GameObject.Instantiate(team.resources.explosionPrefab, transform.position, transform.rotation);
        ParticleSystem explosionParticleSystem = explosionEffect.EnsureComponent<ParticleSystem>();
        ParticleSystem.MainModule explosionMain = explosionParticleSystem.main;
        explosionMain.startLifetime = GameManager.instance.pauseAfterGoalScore;
        explosionMain.startColor = team.teamColor.color;
        explosionParticleSystem.Play();
    }

    public void ResetPlayerPosition()
    {
        if (isNormalPlayer)
        {

            stateManager.AttemptFrozenAfterGoal(
                GetComponent<PlayerMovement>().StartRotateOnly, delegate { }
            );

            transform.position = initialPosition;
            rb2d.rotation = initialRotation;
            renderer.enabled = true;
            collider.enabled = true;
            rb2d.velocity = Vector2.zero;
        }
        if (explosionEffect != null)
        {
            Destroy(explosionEffect);
            explosionEffect = null;
        }
    }

    public void BeginPlayerMovement()
    {
        stateManager.CurrentStateHasFinished();
    }

    public void TrySetTeam(TeamManager team)
    {
        if (this.team == null)
        {
            SetTeam(team);
        }
    }

    public void SetTeam(TeamManager team)
    {
        if (this.team != null)
        {
            this.team.RemoveTeamMember(this);
        }
        this.team = team;
        team.AddTeamMember(this);
        this.FrameDelayCall(() =>
        {
            GetComponent<PlayerDashBehavior>()?.SetPrefabColors();
            GetComponent<LaserGuide>()?.SetLaserGradients();
        }, 2);
        this.FrameDelayCall(() => OnTeamAssigned(team), 2);
    }

    // Use this for initialization
    private void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
        rb2d = GetComponent<Rigidbody2D>();
        stateManager = GetComponent<PlayerStateManager>();
        collider = GetComponent<Collider2D>();

        // Whether this is a "hidden" player that doesn't actually show up/move
        // (i.e. purely sends input events and owns a controller)
        isNormalPlayer = renderer != null && rb2d != null
            && stateManager != null && collider != null;

        if (teamOverride >= 0)
        {
            SetTeam(GameManager.instance.teams[teamOverride]);
        }
        else if ((GameManager.playerTeamsAlreadySelected || GameManager.cheatForcePlayerAssignment)
            && playerNumber >= 0)
        {
            // Dummies have a player number of -1, and shouldn't get a team
            team = GameManager.instance.GetTeamAssignment(this);
            if (team != null && isNormalPlayer)
            {
                SetTeam(team);
            }
        }
        if (isNormalPlayer)
        {
            // initialPosition = transform.position;
            // initalRotation = rb2d.rotation;
        }
        GameManager.instance.players.Add(this);
        // Debug.LogFormat("Assigned player {0} to team {1}", name, team.teamNumber);
    }

    private void OnDestroy()
    {
        if (this.team != null)
        {
            this.team.RemoveTeamMember(this);
        }
        GameManager.instance.players.Remove(this);
    }

}
