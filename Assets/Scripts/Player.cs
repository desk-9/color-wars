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

    public void MakeInvisibleAfterGoal() {
        renderer.enabled = false;
        collider.enabled = false;
        stateManager.AttemptInvisibleAfterGoal(() => {}, () => {});
    }

    public void ResetPlayer() {
        transform.position = initialPosition;
        rb2d.rotation = initalRotation;
        renderer.enabled = true;
        collider.enabled = true;
        stateManager.CurrentStateHasFinished();
    }

    // Use this for initialization
    void Start () {
        renderer = this.EnsureComponent<SpriteRenderer>();
        rb2d = this.EnsureComponent<Rigidbody2D>();
        stateManager = this.EnsureComponent<PlayerStateManager>();
        collider = this.EnsureComponent<Collider2D>();
        team = GameModel.instance.GetTeamAssignment(this);
        renderer.color = team.teamColor;
        initialPosition = transform.position;
        initalRotation = rb2d.rotation;
        Debug.LogFormat("Assigned player {0} to team {1}", name, team.teamNumber);
    }
}
