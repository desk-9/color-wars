using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using IC = InControl;

public class Player : MonoBehaviour {
    public TeamManager team {get; private set;}
    public int playerNumber;
    // teamOverride sets which team a given player will join, overriding all
    // other methods of setting the team if it's non-negative
    public int teamOverride = -1;

    bool isNormalPlayer = true;
    new SpriteRenderer renderer;
    PlayerStateManager stateManager;
    Vector2 initialPosition;
    float initalRotation;
    Rigidbody2D rb2d;
    new Collider2D collider;
    GameObject explosionEffect;

    public void MakeInvisibleAfterGoal() {
        if (isNormalPlayer) {
            renderer.enabled = false;
            collider.enabled = false;
            stateManager.AttemptFrozenAfterGoal(delegate{}, delegate{});
        }
        explosionEffect = GameObject.Instantiate(team.resources.explosionPrefab, transform.position, transform.rotation);
        var explosionParticleSystem = explosionEffect.EnsureComponent<ParticleSystem>();
        var explosionMain = explosionParticleSystem.main;
        explosionMain.startLifetime = GameModel.instance.pauseAfterGoalScore;
        explosionMain.startColor = team.teamColor.color;
        explosionParticleSystem.Play();
    }

    public void ResetPlayerPosition() {
        if (isNormalPlayer) {
            stateManager.AttemptFrozenAfterGoal(delegate{}, delegate{});
            transform.position = initialPosition;
            rb2d.rotation = initalRotation;
            renderer.enabled = true;
            collider.enabled = true;
            rb2d.velocity = Vector2.zero;
        }
        if (explosionEffect != null) {
            Destroy(explosionEffect);
            explosionEffect = null;
        }
    }

    public void BeginPlayerMovement() {
        stateManager.CurrentStateHasFinished();
    }

    public void TrySetTeam(TeamManager team) {
        if (this.team == null) {
            SetTeam(team);
        }
    }

    public void SetTeam(TeamManager team) {
        if (this.team != null) {
            this.team.RemoveTeamMember(this);
        }
        this.team = team;
        team.AddTeamMember(this);
        this.FrameDelayCall(() => {
                GetComponent<PlayerDashBehavior>()?.SetPrefabColors();
                GetComponent<LaserGuide>()?.SetLaserGradients();
            }, 2);

    }

    // Use this for initialization
    void Start () {
        renderer = GetComponent<SpriteRenderer>();
        rb2d = GetComponent<Rigidbody2D>();
        stateManager = GetComponent<PlayerStateManager>();
        collider = GetComponent<Collider2D>();

        // Whether this is a "hidden" player that doesn't actually show up/move
        // (i.e. purely sends input events and owns a controller)
        isNormalPlayer = renderer != null && rb2d != null
            && stateManager != null && collider != null;

        if (teamOverride >= 0) {
            SetTeam(GameModel.instance.teams[teamOverride]);
        } else if ((GameModel.playerTeamsAlreadySelected || GameModel.cheatForcePlayerAssignment)
              && playerNumber >= 0) {
            // Dummies have a player number of -1, and shouldn't get a team
            team = GameModel.instance.GetTeamAssignment(this);
            if (team != null) {
                SetTeam(team);
            }
        }
        if (isNormalPlayer) {
            initialPosition = transform.position;
            initalRotation = rb2d.rotation;
        }
        GameModel.instance.players.Add(this);
        // Debug.LogFormat("Assigned player {0} to team {1}", name, team.teamNumber);
    }

    void OnDestroy() {
        if (this.team != null) {
            this.team.RemoveTeamMember(this);
        }
        GameModel.instance.players.Remove(this);
    }
}
