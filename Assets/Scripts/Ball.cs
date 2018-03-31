using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviour {

    public bool ownable {get; set;} = true;
    public GameObject implosionPrefab;
    public float adjustmentCoefficient;
    public float chargedMassFactor = 1;

    new SpriteRenderer renderer;
    CircleCollider2D circleCollider;
    GameObject target_;
    Goal goal;

    Vector2 start_location;
    BallCarrier owner_;

    new Rigidbody2D rigidbody;
    NotificationCenter notificationCenter;
    TrailRenderer trailRenderer;

    bool charged_ = false;
    float base_mass;

    public BallCarrier lastOwner { get; private set; }

    public GameObject target {
        get { return target_;}
        set
        {
            target_ = value;
        }
    }

    public BallCarrier owner {
        get { return owner_; }
        set {
            lastOwner = owner_;
            if (value != null) {
                target_ = null;
            }
            owner_ = value;

            var message = owner_ == null ? Message.BallIsUnpossessed : Message.BallIsPossessed;
            notificationCenter.NotifyMessage(message, this);
            this.FrameDelayCall(AdjustSpriteToCurrentTeam, 2);
        }
    }

    void SetSpriteToNeutral() {
        renderer.sprite = GameModel.instance.neutralResources.ballSprite;
        trailRenderer.enabled = false;
    }

    void AdjustSpriteToCurrentTeam() {
        if (goal.currentTeam == null) {
            SetSpriteToNeutral();
        } else {
            var newSprite = goal.currentTeam.resources.ballSprite;
            renderer.sprite = newSprite;
            trailRenderer.material.color = goal.currentTeam.teamColor;
        }
    }

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

    void Update() {
        if (target_ != null) {
            var targetVec = target_.transform.position - transform.position;

            if (Mathf.Abs(Vector2.SignedAngle(rigidbody.velocity, targetVec)) > 90f) {
                target_ = null;
                return;
            }

            var adjustmentVector = (Vector2)targetVec.normalized - rigidbody.velocity.normalized;
            rigidbody.AddForce(adjustmentVector * adjustmentCoefficient, ForceMode2D.Impulse);
        }
    }

    public bool IsOwnable() {
        return owner == null && ownable;
    }

    void Start() {
        notificationCenter = GameModel.instance.nc;
        start_location = transform.position;
        trailRenderer = this.EnsureComponent<TrailRenderer>();
        renderer = GetComponentInChildren<SpriteRenderer>();
        circleCollider = this.EnsureComponent<CircleCollider2D>();
        rigidbody = this.EnsureComponent<Rigidbody2D>();
        base_mass = rigidbody.mass;
        goal = GameObject.FindObjectOfType<Goal>();
    }

    public void HandleGoalScore(Color color) {
        var trailRenderer = GetComponent<TrailRenderer>();
        trailRenderer.enabled = false;
        ownable = false;
    }

    public void ResetBall(float? lengthOfEffect = null) {
        circleCollider.enabled = true;
        renderer.enabled = true;

        SetSpriteToNeutral();

        transform.position = start_location;

        trailRenderer.enabled = false;
        ownable = true;
        rigidbody.velocity = Vector2.zero;
        if (lengthOfEffect != null) {
            StartCoroutine(ImplosionEffect(lengthOfEffect.Value));
        }
        charged = false;
        target_ = null;
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
            target_ = null;
        }
    }
}
