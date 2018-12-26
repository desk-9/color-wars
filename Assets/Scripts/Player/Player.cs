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

    private new SpriteRenderer renderer;
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
        renderer.enabled = false;
        collider.enabled = false;
        StateManager.TransitionToState(State.FrozenAfterGoal);

        explosionEffect = GameObject.Instantiate(Team.resources.explosionPrefab, transform.position, transform.rotation);
        ParticleSystem explosionParticleSystem = explosionEffect.EnsureComponent<ParticleSystem>();
        ParticleSystem.MainModule explosionMain = explosionParticleSystem.main;
        explosionMain.startLifetime = GameManager.Settings.PauseAfterGoalScore;
        explosionMain.startColor = Team.TeamColor.color;
        explosionParticleSystem.Play();
    }

    public void ResetPlayerAfterGoal()
    {
        StateManager.TransitionToState(State.StartOfMatch);
        renderer.enabled = true;
        collider.enabled = true;

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
        StateManager = GetComponent<PlayerStateManager>();
        collider = GetComponent<Collider2D>();

        if (teamOverride >= 0)
        {
            SetTeam(GameManager.Instance.Teams[teamOverride]);
        }
        else if ((GameManager.playerTeamsAlreadySelected || GameManager.cheatForcePlayerAssignment)
            && playerNumber >= 0)
        {
            // Dummies have a player number of -1, and shouldn't get a team
            Team = GameManager.Instance.GetTeamAssignment(this);
            if (Team != null)
            {
                SetTeam(Team);
            }
        }

        GameManager.NotificationManager.CallOnMessage(Message.PlayerAssignedPlayerNumber, HandlePlayerNumberAssigned);
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
