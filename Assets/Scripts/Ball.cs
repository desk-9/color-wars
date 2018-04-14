using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviour {

    public bool ownable {get; set;} = true;
    public GameObject implosionPrefab;
    new public SpriteRenderer renderer;

    CircleCollider2D circleCollider;
    Goal goal;

    Vector2 start_location;
    BallCarrier owner_;
    BallFillColor ballFill;

    public new Rigidbody2D rigidbody;
    NotificationCenter notificationCenter;
    TrailRenderer trailRenderer;

    Color neutralColor = Color.white;
    Color currentColor;

    public BallCarrier lastOwner { get; private set; }

    public BallCarrier owner {
        get { return owner_; }
        set {
            if (owner_ != null) {
                lastOwner = owner_;
            }
            owner_ = value;

            var message = owner_ == null ? Message.BallIsUnpossessed : Message.BallIsPossessed;
            notificationCenter.NotifyMessage(message, this);
            if (!this.isActiveAndEnabled) {
                return;
            }
            if (owner_ != null) {
                this.FrameDelayCall(AdjustSpriteToCurrentTeam, 2);
            } else {
                trailRenderer.enabled = false;
            }
        }
    }

    void SetColor(Color to_, bool fill) {
        Gradient gradient = new Gradient();
        gradient.SetKeys(new GradientColorKey[] { new GradientColorKey(to_, 0.0f), new GradientColorKey(to_, 1.0f) },
                         new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(0f, 1.0f) }
                         );

        trailRenderer.colorGradient = gradient;
        this.FrameDelayCall(EnableTrail, 5);
        if (fill) {
            renderer.color = to_;
            ballFill.EnableAndSetColor(to_);
        } else {
            renderer.color = Color.Lerp(to_, Color.white, .6f);
            ballFill.DisableFill();
        }
    }

    void EnableTrail() {
        trailRenderer.enabled = true;
    }

    // This is for resets
    void SetSpriteToNeutral() {
        SetColor(neutralColor, false);
    }

    Color ColorFromBallCarrier(BallCarrier carrier) {
        return carrier.EnsureComponent<Player>().team.teamColor.color;
    }

    void AdjustSpriteToCurrentTeam() {
        // Happens if player shoots a frame after pickup
        if (owner == null) {
            Debug.Assert(lastOwner != null);
            var lastOwnerColor = ColorFromBallCarrier(lastOwner);
            var fill = goal?.currentTeam != null && goal?.currentTeam.teamColor == lastOwnerColor;
            SetColor(lastOwnerColor, fill);
            return;
        }

        var currentOwnerColor = ColorFromBallCarrier(owner);

        if (goal?.currentTeam != null &&
            goal?.currentTeam.teamColor == currentOwnerColor) {
            SetColor(currentOwnerColor, true);
        } else {
            SetColor(currentOwnerColor, false);
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
        goal = GameObject.FindObjectOfType<Goal>();
        ballFill = this.GetComponentInChildren<BallFillColor>();
        currentColor = Color.white;
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
}
