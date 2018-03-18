using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviour {

    public BallCarrier owner { get; set; }
    public bool ownable {get; set;} = true;
    public GameObject implosionPrefab;

    Vector2 start_location;
    Rigidbody2D rb2d;
    new SpriteRenderer renderer;
    CircleCollider2D circleCollider;

    public bool IsOwnable() {
        return owner == null && ownable;
    }

    void Start() {
        start_location = transform.position;
        rb2d = this.EnsureComponent<Rigidbody2D>();
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

    public void ResetBall(float? lengthOfEffect = null) {
        circleCollider.enabled = true;
        renderer.enabled = true;
        transform.position = start_location;
        ownable = true;
        rb2d.velocity = Vector2.zero;
        if (lengthOfEffect != null) {
            StartCoroutine(ImplosionEffect(lengthOfEffect.Value));
        }
    }

    IEnumerator ImplosionEffect(float duration) {
        var explosion = GameObject.Instantiate(implosionPrefab, transform.position, transform.rotation);
        var explosionPS = explosion.EnsureComponent<ParticleSystem>();
        var explosionMain = explosionPS.main;
        explosionMain.duration = duration;
        explosionMain.startColor = renderer.color;
        explosionPS.Play();

        var startingScale = transform.localScale;
        float elapsedTime = 0f;
        while(elapsedTime < duration) {
            transform.localScale = Vector3.Lerp(Vector3.zero, startingScale, elapsedTime/duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
}
