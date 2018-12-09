using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviourPunCallbacks
{
    public new SpriteRenderer renderer;
    public new Rigidbody2D rigidbody;
    [SerializeField]
    private GameObject implosionPrefab;

    private CircleCollider2D circleCollider;
    private Goal goal;
    private Vector2 start_location;
    private BallFillColor ballFill;
    private NotificationManager notificationManager;
    private TrailRenderer trailRenderer;
    private float speedOnShoot;
    private Color neutralColor = Color.white;
    private int relevantCollisionLayers;

    /// <summary>
    /// The owner before the current one, or just the last owner if there is no
    /// current owner
    /// </summary>
    public BallCarrier LastOwner { get; private set; }

    /// <summary>
    /// In certain situations (like after a goal is scored, or while the ball
    /// position is being reset), the ball is not ownable. This represents that
    /// </summary>
    public bool Ownable { get; set; } = true;

    public Vector2 CurrentPosition
    {
        get { return transform.position; }
    }


    /// <summary>
    /// The current owner of the ball if there is one, null otherwise
    /// </summary>
    private BallCarrier owner_;
    public BallCarrier Owner
    {
        get { return owner_; }
        set
        {
            if (owner_ != null)
            {
                LastOwner = owner_;
            }
            owner_ = value;
            rigidbody.mass = owner_ == null ? 0.1f : 1000;
            Message message = owner_ == null ? Message.BallIsUnpossessed : Message.BallIsPossessed;
            rigidbody.angularVelocity = 0f;
            notificationManager.NotifyMessage(message, gameObject);
            // TODO dkonik: You need to stop listening to this event at some point idiot.
            // Also, maybe this should just be private
            notificationManager.CallOnStateStart(State.ShootBall_micro, HandlePlayerShotBall);
            if (!this.isActiveAndEnabled)
            {
                return;
            }
            // TODO dkonik: This used to be where the color of the ball was set. 
            // No longer! But figure out where that should go. Part of it is in 
            // handle charge change. But we also want to change it even if not charged.
            // Possession manager should probably have a possession event change too, and
            // that is what *most* things listen to...but idk.
        }
    }

    private void HandlePlayerShotBall(Player player)
    {
        AudioManager.instance.ShootBallSound.Play(.5f);

        // TODO anyone: This is where we could do something like handling turning off of the 
        // photon transform view component, since we know which way the ball will be heading for
        // a little bit.
        ShootBallInformation information = player.StateManager.CurrentStateInformation as ShootBallInformation;

        // What we should (could?) do here is interpolate, based off of information.EventTimeStamp,
        // the current position of the ball

        // This was the old code 
        //Vector3 shotDirection = information.Direction;
        //Rigidbody2D ballRigidBody = ball.EnsureComponent<Rigidbody2D>();
        //ballRigidBody.velocity = shotDirection.normalized * shotSpeed;
    }

    private void SetColor(Color to_, bool fill)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(to_, 0.0f),
                new GradientColorKey(to_, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0.0f),
                new GradientAlphaKey(0f, 1.0f)
            });

        trailRenderer.colorGradient = gradient;
        
        // I think we do this because if you instantly enable it, there is a short
        // trail that appears as the ball lerps to the new owners nose
        this.FrameDelayCall(EnableTrail, 5);

        if (fill)
        {
            renderer.color = to_;
            ballFill.EnableAndSetColor(to_);
        }
        else
        {
            renderer.color = Color.Lerp(to_, Color.white, .6f);
            ballFill.DisableFill();
        }
    }

    private void EnableTrail()
    {
        trailRenderer.enabled = true;
    }

    // This is for resets
    private void SetSpriteToNeutral()
    {
        SetColor(neutralColor, false);
    }

    private void Start()
    {
        notificationManager = GameManager.instance.notificationManager;
        start_location = transform.position;
        trailRenderer = this.EnsureComponent<TrailRenderer>();
        renderer = GetComponentInChildren<SpriteRenderer>();
        circleCollider = this.EnsureComponent<CircleCollider2D>();
        rigidbody = this.EnsureComponent<Rigidbody2D>();
        goal = GameObject.FindObjectOfType<Goal>();
        ballFill = this.GetComponentInChildren<BallFillColor>();
        relevantCollisionLayers = LayerMask.GetMask("Wall", "TronWall", "Goal", "PlayerBlocker");

        GameManager.instance.notificationManager.CallOnMessage(
            Message.BallIsUnpossessed, HandleUnpossesion
        );
        GameManager.instance.notificationManager.CallOnMessage(
            Message.ChargeChanged, HandleChargeChanged
        );
        GameManager.instance.notificationManager.CallOnMessage(
            Message.GoalScored, HandleGoalScore
        );
    }

    private void HandleUnpossesion()
    {
        if (this == null || !this.enabled) return;

        this.FrameDelayCall(() =>
        {
            if (this == null || !this.enabled) return;
            speedOnShoot = rigidbody.velocity.magnitude;
        });
    }

    private void HandleChargeChanged()
    {
        // TODO dkonik: This was in the old adjustSpriteToCurrentTeam function...
        // is it still needed?
        //// Happens if player shoots a frame after pickup
        //if (Owner == null)
        //{
        //    Debug.Assert(LastOwner != null);
        //    Color lastOwnerColor = ColorFromBallCarrier(LastOwner);
        //    bool fill = goal?.currentTeam != null && goal?.currentTeam.teamColor == lastOwnerColor;
        //    SetColor(lastOwnerColor, fill);
        //    return;
        //}
        TeamManager newTeam = GameManager.instance.PossessionManager.CurrentTeam;
        if (newTeam == null)
        {
            throw new Exception("Would not expect the current team to be null in charge changed");
        }
        
        if (GameManager.instance.PossessionManager.IsCharged)
        {
            SetColor(GameManager.instance.PossessionManager.CurrentTeam.teamColor, true);
        } else
        {
            SetColor(GameManager.instance.PossessionManager.CurrentTeam.teamColor, false);
        }
    }

    private void HandleGoalScore()
    {
        trailRenderer.enabled = false;
        Ownable = false;
    }

    public void ResetBall(float? lengthOfEffect = null)
    {
        // Reset values 
        circleCollider.enabled = true;
        renderer.enabled = true;
        SetSpriteToNeutral();
        transform.position = start_location;
        trailRenderer.enabled = false;
        Ownable = true;
        rigidbody.velocity = Vector2.zero;
        Owner = null;
        LastOwner = null;

        // Start Spawn effect
        if (lengthOfEffect != null)
        {
            StartCoroutine(ImplosionEffect(lengthOfEffect.Value));
        }
    }

    private IEnumerator ImplosionEffect(float duration)
    {
        GameObject explosion = GameObject.Instantiate(implosionPrefab, transform.position, transform.rotation);
        ParticleSystem explosionPS = explosion.EnsureComponent<ParticleSystem>();
        ParticleSystem.MainModule explosionMain = explosionPS.main;
        explosionMain.duration = duration;
        explosionMain.startColor = renderer.color;
        explosionPS.Play();

        Vector3 startingScale = transform.localScale;
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            transform.localScale = Vector3.Lerp(Vector3.zero, startingScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private void FixedUpdate()
    {
        rigidbody.rotation = Utility.NormalizeDegree(rigidbody.rotation);
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if (relevantCollisionLayers == (relevantCollisionLayers | 1 << collision.gameObject.layer))
        {
            this.FrameDelayCall(
                () =>
                {
                    if (rigidbody.velocity.magnitude > speedOnShoot)
                    {
                        Debug.LogWarning("Prevented ball from speeding up after wall");
                        rigidbody.velocity = rigidbody.velocity.normalized * speedOnShoot;
                    }
                }
                );
        }
    }
}
