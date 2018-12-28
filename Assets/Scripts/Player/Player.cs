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

    // It's now possible for the network to force a certain sprite/roster number
    // for a player
    public void SetTeam(TeamManager team, int forceSpriteNumber = -1)
    {
        if (this.Team == team) {
            return;
        }
        if (this.Team != null)
        {
            this.Team.RemoveTeamMember(this);
        }
        this.Team = team;
        team.AddTeamMember(this, forceSpriteNumber);
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
        // The spawn point manager sends the player number in the initialization
        // message, allowing non-locally-controlled players to spawn with the
        // right player number
        if (photonView.InstantiationData != null) {
            playerNumber = (int) photonView.InstantiationData[0];
        }
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
            var assignedTeam = GameManager.Instance.GetTeamAssignment(this);
            if (assignedTeam != null)
            {
                SetTeam(assignedTeam);
            }
        } else {
            Utility.Print("Player", playerNumber, "has no team on spawn", LogLevel.Warning);
        }

        GameManager.Instance.players.Add(this);
        // Under the current flow, players should ONLY be spawned after already
        // having been assigned to an actor, so before the start function. Thus
        // this handing function is now just part of startup rather than
        // triggerable at any time.
        //
        // TODO clean up name of function
        HandlePlayerNumberAssigned();
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
            // TODO figure out right state transition here. Right now this is
            // here to allow players in the lobby to move, since they don't have
            // a countdown, but pretty sure current behavior is messed up.
            BeginPlayerMovement();
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
