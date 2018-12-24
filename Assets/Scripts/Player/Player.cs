using UnityEngine;
using UtilityExtensions;
using Photon.Realtime;
using Photon.Pun;

public class Player : MonoBehaviourPunCallbacks
{
    public delegate void OnTeamAssignedCallback(TeamManager team);
    public OnTeamAssignedCallback OnTeamAssigned = delegate { };

    public PlayerStateManager StateManager { get; private set; }

    public TeamManager Team { get; private set; }
    public int playerNumber;
    // teamOverride sets which team a given player will join, overriding all
    // other methods of setting the team if it's non-negative
    public int teamOverride = -1;
    public Vector2 initialPosition;
    public float initialRotation;

    private bool isNormalPlayer = true;
    private new SpriteRenderer renderer;
    private Rigidbody2D rb2d;
    private new Collider2D collider;
    private GameObject explosionEffect;
    private PlayerMovement playerMovement;

    public PlayerMovement PlayerMovement
    {
        get
        {
            // Lazy load
            if (playerMovement == null)
            {
                playerMovement = GetComponent<PlayerMovement>()
                    .ThrowIfNull("Player could not find PlayerMovement component");
            }
            return playerMovement;
        }
    }

    public void CallAsSoonAsTeamAssigned(OnTeamAssignedCallback callback)
    {
        if (Team != null)
        {
            callback(Team);
        }
        OnTeamAssigned += callback;
    }

    public void MakeInvisibleAfterGoal()
    {
        if (isNormalPlayer)
        {
            renderer.enabled = false;
            collider.enabled = false;

            StateManager.TransitionToState(State.FrozenAfterGoal);
        }

        explosionEffect = GameObject.Instantiate(Team.resources.explosionPrefab, transform.position, transform.rotation);
        ParticleSystem explosionParticleSystem = explosionEffect.EnsureComponent<ParticleSystem>();
        ParticleSystem.MainModule explosionMain = explosionParticleSystem.main;
        explosionMain.startLifetime = GameManager.Instance.pauseAfterGoalScore;
        explosionMain.startColor = Team.TeamColor.color;
        explosionParticleSystem.Play();
    }

    public void ResetPlayerPosition()
    {
        if (isNormalPlayer)
        {
            StateManager.TransitionToState(State.StartOfMatch);

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
        StateManager.TransitionToState(State.NormalMovement);
    }

    public void TrySetTeam(TeamManager team)
    {
        if (this.Team == null)
        {
            SetTeam(team);
        }
    }

    public void SetTeam(TeamManager team)
    {
        if (this.Team != null)
        {
            this.Team.RemoveTeamMember(this);
        }
        this.Team = team;
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
        StateManager = GetComponent<PlayerStateManager>();
        collider = GetComponent<Collider2D>();

        // Whether this is a "hidden" player that doesn't actually show up/move
        // (i.e. purely sends input events and owns a controller)
        isNormalPlayer = renderer != null && rb2d != null
            && StateManager != null && collider != null;

        if (teamOverride >= 0)
        {
            SetTeam(GameManager.Instance.Teams[teamOverride]);
        }
        else if ((GameManager.playerTeamsAlreadySelected || GameManager.cheatForcePlayerAssignment)
            && playerNumber >= 0)
        {
            // Dummies have a player number of -1, and shouldn't get a team
            Team = GameManager.Instance.GetTeamAssignment(this);
            if (Team != null && isNormalPlayer)
            {
                SetTeam(Team);
            }
        }

        GameManager.Instance.NotificationManager.CallOnMessage(Message.PlayerAssignedPlayerNumber, HandlePlayerNumberAssigned);
        GameManager.Instance.players.Add(this);
    }

    /// <summary>
    /// Deal with state changes due to being owned (or not owned) by the
    /// local network player
    /// </summary>
    public void HandlePlayerNumberAssigned()
    {
        if (NetworkPlayerManager.Instance.LocalOwnsPlayer(playerNumber))
        {
            photonView.TransferOwnership(PhotonNetwork.LocalPlayer);
            var controls = GetComponent<PlayerControls>();
            controls.AskForDevice();
        }
    }

    private void OnDestroy()
    {
        if (this.Team != null)
        {
            this.Team.RemoveTeamMember(this);
        }
        GameManager.Instance.players.Remove(this);
    }

}
