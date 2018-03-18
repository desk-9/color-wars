using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class Player : MonoBehaviour {

    public TeamManager team {get; private set;}

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
    }

    public void BeginPlayerMovement() {
        stateManager.CurrentStateHasFinished();
    }

    // Use this for initialization
    void Start () {
        renderer = this.EnsureComponent<SpriteRenderer>();
        rb2d = this.EnsureComponent<Rigidbody2D>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        collider = this.EnsureComponent<Collider2D>();
        explosion = GetComponent<ParticleSystem>();
        team = GameModel.instance.GetTeamAssignment(this);
        renderer.color = team.teamColor;
        initialPosition = transform.position;
        initalRotation = rb2d.rotation;
        Debug.LogFormat("Assigned player {0} to team {1}", name, team.teamNumber);
    }
}
