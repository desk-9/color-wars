using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviour {

    public BallCarrier owner { get; set; }
    public bool ownable {get; set;} = true;

    Vector2 start_location;
    Rigidbody2D rb2d;
    new SpriteRenderer renderer;
    CircleCollider2D circleCollider;
    ParticleSystem explosion;

    public bool IsOwnable() {
        return owner == null && ownable;
    }

    void Start() {
        start_location = transform.position;
        rb2d = this.EnsureComponent<Rigidbody2D>();
        explosion = GetComponent<ParticleSystem>();
        renderer = this.EnsureComponent<SpriteRenderer>();
        circleCollider = this.EnsureComponent<CircleCollider2D>();
    }

    public void HandleGoalScore(Color color) {
        // var explosionMain = explosion.main;
        // explosionMain.duration = GameModel.instance.pauseAfterGoalScore;
        // explosionMain.startColor = color;
        // explosion.Play();

        // rb2d.velocity = Vector2.zero;
        // renderer.enabled = false;
        // circleCollider.enabled = false;
        ownable = false;
    }

    public void ResetBall() {
        circleCollider.enabled = true;
        renderer.enabled = true;
        transform.position = start_location;
        ownable = true;
        rb2d.velocity = Vector2.zero;
    }
}
