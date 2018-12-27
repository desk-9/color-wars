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

    private Vector2 startLocation;
    private NotificationManager notificationManager;
    private float speedOnShoot;
    private int relevantCollisionLayers;
    private new Rigidbody2D rigidbody;
    private PhysicsTransformView physicsTransformView;

    /// <summary>
    /// The owner before the current one, or just the last owner if there is no
    /// current owner
    /// </summary>
    public Player LastOwner { get; private set; }

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
    private Player owner_;
    public Player Owner
    {
        get { return owner_; }
        set
        {
            if (owner_ != null)
            {
                LastOwner = owner_;
                physicsTransformView.enabled = true;
            } else {
                physicsTransformView.enabled = false;
            }
            owner_ = value;
            rigidbody.mass = owner_ == null ? 0.1f : 1000;

            rigidbody.angularVelocity = 0f;

            if (!this.isActiveAndEnabled)
            {
                return;
            }
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
        NormalMovementInformation information = player.StateManager.CurrentStateInformation_Exn<NormalMovementInformation>();
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

    private void Start()
    {
        notificationManager = GameManager.NotificationManager;
        startLocation = transform.position;
        renderer = GetComponentInChildren<SpriteRenderer>();
        rigidbody = this.EnsureComponent<Rigidbody2D>();
        relevantCollisionLayers = LayerMask.GetMask("Wall", "TronWall", "Goal", "PlayerBlocker");
        physicsTransformView = this.EnsureComponent<PhysicsTransformView>();

        notificationManager.CallOnMessage(
            Message.BallIsUnpossessed, HandleUnpossesion
        );
        notificationManager.CallOnMessage(
            Message.GoalScored, HandleGoalScore
        );
        notificationManager.CallOnMessage(Message.BallWentOutOfBounds, () => ResetBall(false));
        notificationManager.CallOnMessage(Message.ResetAfterGoal, () => ResetBall(true));
        notificationManager.CallOnStateStart(State.Possession, HandlePossession);
        notificationManager.CallOnStateEnd(State.Possession, HandlePossessionLost);
        notificationManager.CallOnStateStart(State.NormalMovement, HandlePlayerShotBall);
    }

    private void HandlePossessionLost(Player player)
    {
        Owner = null;
    }

    private void HandlePossession(Player player)
    {
        // TODO dkonik: Probably more to do here
        rigidbody.velocity = Vector2.zero;
        rigidbody.angularVelocity = 0;
        Owner = player;
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

    private void HandleGoalScore()
    {
        Ownable = false;
    }

    private void ResetBall(bool doSpawnAnimation)
    {
        // Reset values
        transform.position = startLocation;
        Ownable = true;
        rigidbody.velocity = Vector2.zero;
        Owner = null;
        LastOwner = null;

        // Start Spawn effect
        if (doSpawnAnimation)
        {
            StartCoroutine(ImplosionEffect(GameManager.Settings.LengthOfBallSpawnAnimation));
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
