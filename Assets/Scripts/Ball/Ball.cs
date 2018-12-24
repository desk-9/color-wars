using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviourPunCallbacks
{
    public new SpriteRenderer renderer;
    
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
    private new Rigidbody2D rigidbody;

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

    private float _radius = -1f;
    public float Radius
    {
        get
        {
            if (_radius == -1f)
            {
                _radius = GetComponent<CircleCollider2D>().bounds.extents.x;
            }
            return _radius;
        }
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

    public void TakeOwnership()
    {
        photonView.RequestOwnership();
    }

    public void MoveTo(Vector2 newPosition)
    {
        rigidbody.MovePosition(newPosition);
    }

    private void HandlePlayerShotBall(Player player)
    {
        NormalMovementInformation information = player.StateManager.CurrentStateInformation as NormalMovementInformation;
        // If we didn't shoot the ball, just return
        if (!information.ShotBall)
        {
            return;
        }

        AudioManager.instance.ShootBallSound.Play(.5f);
       
        if (photonView.IsMine)
        {
            // TODO dkonik: Do more here, interp based on the timestamp and ball start
            // pos, but I am being lazy right now just to see how this works
            rigidbody.velocity = information.Velocity;
        }
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
        notificationManager = GameManager.instance.NotificationManager;
        start_location = transform.position;
        trailRenderer = this.EnsureComponent<TrailRenderer>();
        renderer = GetComponentInChildren<SpriteRenderer>();
        circleCollider = this.EnsureComponent<CircleCollider2D>();
        rigidbody = this.EnsureComponent<Rigidbody2D>();
        goal = GameObject.FindObjectOfType<Goal>();
        ballFill = this.GetComponentInChildren<BallFillColor>();
        relevantCollisionLayers = LayerMask.GetMask("Wall", "TronWall", "Goal", "PlayerBlocker");

        notificationManager.CallOnMessage(
            Message.BallIsUnpossessed, HandleUnpossesion
        );
        notificationManager.CallOnMessage(
            Message.ChargeChanged, HandleChargeChanged
        );
        notificationManager.CallOnMessage(
            Message.GoalScored, HandleGoalScore
        );
        notificationManager.CallOnStateStart(State.Possession, HandlePossession);
        notificationManager.CallOnStateStart(State.NormalMovement, HandlePlayerShotBall);
    }

    private void HandlePossession(Player player)
    {
        // TODO dkonik: Probably more to do here
        rigidbody.velocity = Vector2.zero;
        rigidbody.angularVelocity = 0;
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
            SetColor(GameManager.instance.PossessionManager.CurrentTeam.TeamColor, true);
        } else
        {
            SetColor(GameManager.instance.PossessionManager.CurrentTeam.TeamColor, false);
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
