using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviour {

    public bool ownable {get; set;} = true;
    public GameObject implosionPrefab;

    new SpriteRenderer renderer;
    CircleCollider2D circleCollider;

    Vector2 start_location;
    BallCarrier owner_;
    public BallCarrier owner {
        get { return owner_; }
        set {
            lastOwner = owner_;
            owner_ = value;
        }
    }
    public BallCarrier lastOwner { get; private set; }
    public float chargedMassFactor = 1;

    new Rigidbody2D rigidbody;

    bool charged_ = false;
    float base_mass;
    public bool charged {
        get {
            return charged_;
        }
        set {
            charged_ = value;
            if (charged_) {
                rigidbody.mass = base_mass * chargedMassFactor;
            } else {
                rigidbody.mass = base_mass;
            }
        }
    }

    public bool IsOwnable() {
        return owner == null && ownable;
    }

    void Start() {
        start_location = transform.position;
        renderer = this.EnsureComponent<SpriteRenderer>();
        circleCollider = this.EnsureComponent<CircleCollider2D>();
        rigidbody = this.EnsureComponent<Rigidbody2D>();
        base_mass = rigidbody.mass;
    }

    public void HandleGoalScore(Color color) {
        // var explosionMain = explosion.main;
        // explosionMain.duration = GameModel.instance.pauseAfterGoalScore;
        // explosionMain.startColor = color;
        // explosion.Play();

        // rb2d.velocity = Vector2.zero;
        // renderer.enabled = false;
        // circleCollider.enabled = false;
        var trailRenderer = GetComponent<TrailRenderer>();
        trailRenderer.enabled = false;
        ownable = false;
    }

    public void ResetBall(float? lengthOfEffect = null) {
        circleCollider.enabled = true;
        renderer.enabled = true;

        transform.position = start_location;

        var trailRenderer = GetComponent<TrailRenderer>();
        trailRenderer.enabled = true;
        ownable = true;
        rigidbody.velocity = Vector2.zero;
        if (lengthOfEffect != null) {
            StartCoroutine(ImplosionEffect(lengthOfEffect.Value));
        }
        charged = false;
        owner = null;
        lastOwner = null;
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

    void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Wall")) {
            charged = false;
        }
    }
}
