﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using IC = InControl;

public class Player : MonoBehaviour {
    public GameObject inline;
    public TeamManager team {get; private set;}
    public int playerNumber;
    public Dictionary<string, string> teamToSprite = new Dictionary<string, string>() {
        {"Fire", "Sprites/FirePlayer"},
        {"Ice", "Sprites/IcePlayer"}
    };

    new SpriteRenderer renderer;
    PlayerStateManager stateManager;
    Vector2 initialPosition;
    float initalRotation;
    Rigidbody2D rb2d;
    new Collider2D collider;
    ParticleSystem explosion;

    public void MakeInvisibleAfterGoal() {
        renderer.enabled = false;
        collider.enabled = false;
        stateManager.AttemptFrozenAfterGoal(delegate{}, delegate{});

        var explosionMain = explosion.main;
        explosionMain.startLifetime = GameModel.instance.pauseAfterGoalScore;
        explosionMain.startColor = team.teamColor.color;
        explosion.Play();
    }

    public void ResetPlayerPosition() {
        stateManager.AttemptFrozenAfterGoal(delegate{}, delegate{});
        transform.position = initialPosition;
        rb2d.rotation = initalRotation;
        renderer.enabled = true;
        collider.enabled = true;
        rb2d.velocity = Vector2.zero;
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
        renderer.color = team.teamColor;
        team.AddTeamMember(this);
        GetComponent<PlayerDashBehavior>()?.SetPrefabColors();
    }

    // Use this for initialization
    void Start () {
        renderer = this.EnsureComponent<SpriteRenderer>();
        rb2d = this.EnsureComponent<Rigidbody2D>();
        stateManager = GetComponent<PlayerStateManager>();
        collider = this.EnsureComponent<Collider2D>();
        explosion = GetComponent<ParticleSystem>();
        if ((GameModel.playerTeamsAlreadySelected || GameModel.cheatForcePlayerAssignment)
              && playerNumber > 0) {
            // Dummies have a player number of -1, and shouldn't get a team
            team = GameModel.instance.GetTeamAssignment(this);
            this.FrameDelayCall(
                () => GetComponent<PlayerDashBehavior>()?.SetPrefabColors(),
                2);
            var teamSprite = Resources.Load<Sprite>(teamToSprite[team.teamColor.name]);
            renderer.sprite = teamSprite;
        }
        initialPosition = transform.position;
        initalRotation = rb2d.rotation;
        GameModel.instance.players.Add(this);
        // Debug.LogFormat("Assigned player {0} to team {1}", name, team.teamNumber);
    }

    void Update() {
        var device = GetComponent<PlayerMovement>()?.GetInputDevice();
        if (device != null && device.GetControl(IC.InputControlType.Action1).WasPressed) {
            Utility.TutEvent("Done", this);
        }
    }
}
