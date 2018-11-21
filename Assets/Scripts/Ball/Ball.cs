using Photon.Pun;
using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviourPun, IPunObservable
{
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
    private new SpriteRenderer renderer;

    /// <summary>
    /// The owner before the current owner. If the ball is not currently possessed, then
    /// this is just the last owner. If the ball is possessed, then this will be the owner
    /// before the current one.
    /// </summary>
    public BallCarrier LastOwner { get; private set; }

    /// <summary>
    /// The ball may not be ownable in certain states, for example
    /// after a goal is scored, or while it is being reset after a goal.
    /// </summary>
    public bool IsOwnable { get; set; } = true;

    #region NetworkingAndPhysics

    private struct NetworkedInformation
    {
        public bool isDirty;
        public Vector2 position;
        public Vector2 velocity;
        public float rotation;
        public float angularVelocity;
        public double timestampOfMessage;
    }

    // TODO dkonik: This is ugly, but just testing to get it working
    private Vector2 requestedVelocity;
    private bool velocityWasRequested = false;

    private NetworkedInformation networkedInformation = new NetworkedInformation();
    private bool stopBallForLocalPossession = false;

    /// <summary>
    /// Moves the ball to the new position if the ball is owned by the local user.
    /// Should only be called on FixedUpdate
    /// </summary>
    /// <param name="newPosition"></param>
    public void NetworkedMoveToPosition(Vector2 newPosition)
    {
        if (photonView.IsMine)
        {
            rigidbody.MovePosition(newPosition);
        }
    }

    public void NetworkedSetVelocity(Vector2 velocity)
    {
        if (photonView.IsMine)
        {
            velocityWasRequested = true;
            requestedVelocity = velocity;
        }
    }
    #endregion

    #region StateInformationGetters
    public Vector2 CurrentPosition
    {
        get
        {
            return rigidbody.position;
        }
    }

    public Color CurrentColor
    {
        get
        {
            return renderer.color;
        }
    }
    #endregion

    /// <summary>
    /// In certain situations (like after a goal is scored, or while the ball
    /// position is being reset), the ball is not ownable. This represents that
    /// </summary>
    public bool Ownable { get; set; } = true;

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
                stopBallForLocalPossession = true;
            }
            owner_ = value;

            Message message = owner_ == null ? Message.BallIsUnpossessed : Message.BallIsPossessed;

            notificationManager.NotifyMessage(message, gameObject);

            if (!this.isActiveAndEnabled)
            {
                return;
            }

            if (owner_ != null)
            {
                this.FrameDelayCall(AdjustSpriteToCurrentTeam, 2);
            }
            else
            {
                trailRenderer.enabled = false;
            }
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

    private Color ColorFromBallCarrier(BallCarrier carrier)
    {
        TeamManager carrierTeam = carrier.EnsureComponent<Player>().team;
        return carrierTeam != null ? carrierTeam.teamColor.color : Color.white;
    }

    private void AdjustSpriteToCurrentTeam()
    {
        // Happens if player shoots a frame after pickup
        if (Owner == null)
        {
            Debug.Assert(LastOwner != null);
            Color lastOwnerColor = ColorFromBallCarrier(LastOwner);
            bool fill = goal?.currentTeam != null && goal?.currentTeam.teamColor == lastOwnerColor;
            SetColor(lastOwnerColor, fill);
            return;
        }

        Color currentOwnerColor = ColorFromBallCarrier(Owner);

        if (goal?.currentTeam != null &&
            goal?.currentTeam.teamColor == currentOwnerColor)
        {
            SetColor(currentOwnerColor, true);
        }
        else
        {
            SetColor(currentOwnerColor, false);
        }
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

    public void HandleGoalScore(Color color)
    {
        TrailRenderer trailRenderer = GetComponent<TrailRenderer>();
        trailRenderer.enabled = false;
        IsOwnable = false;
    }

    public void ResetBall(float? lengthOfEffect = null)
    {
        // Reset values 
        circleCollider.enabled = true;
        renderer.enabled = true;
        SetSpriteToNeutral();
        transform.position = start_location;
        trailRenderer.enabled = false;
        IsOwnable = true;
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

    // TODO dkonik: This was here to fix an issue where the ball would sometimes randomly 
    // bounce off a wall super fast, but we can no longer just set the velocity willy nilly like this.
    // Maybe this is fixed in the newer unity. If not, actually figure out why.
    //private void OnCollisionEnter2D(Collision2D collision)
    //{
    //    if (relevantCollisionLayers == (relevantCollisionLayers | 1 << collision.gameObject.layer))
    //    {
    //        this.FrameDelayCall(
    //            () =>
    //            {
    //                if (rigidbody.velocity.magnitude > speedOnShoot)
    //                {
    //                    Debug.LogWarning("Prevented ball from speeding up after wall");
    //                    rigidbody.velocity = rigidbody.velocity.normalized * speedOnShoot;
    //                }
    //            }
    //            );
    //    }
    //}

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(this.rigidbody.position);
            stream.SendNext(this.rigidbody.velocity);
            stream.SendNext(this.rigidbody.rotation);
            stream.SendNext(this.rigidbody.angularVelocity);
        }
        else
        {
            // We may or may not get this information on a physics frame, so just
            // mark as dirty and update the position in FixedUpdate
            networkedInformation.isDirty = true;
            networkedInformation.position = (Vector2)stream.ReceiveNext();
            networkedInformation.velocity = (Vector2)stream.ReceiveNext();
            networkedInformation.rotation = (float)stream.ReceiveNext();
            networkedInformation.angularVelocity = (float)stream.ReceiveNext();
            networkedInformation.timestampOfMessage = info.timestamp;
        }
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            rigidbody.rotation = Utility.NormalizeDegree(rigidbody.rotation);
            if (stopBallForLocalPossession)
            {
                rigidbody.angularVelocity = 0f;
                rigidbody.velocity = Vector2.zero;
                stopBallForLocalPossession = false;
            } else if (velocityWasRequested)
            {
                velocityWasRequested = false;
                rigidbody.velocity = requestedVelocity;
            }
        } else
        {
            if (networkedInformation.isDirty)
            {
                // TODO dkonik: Take into account bouncing off a wall
                rigidbody.velocity = networkedInformation.velocity;
                rigidbody.rotation = networkedInformation.rotation;
                rigidbody.angularVelocity = networkedInformation.angularVelocity;

                // New position based on velocity
                Vector2 newPosition = networkedInformation.position;
                float lag = Mathf.Abs((float)(PhotonNetwork.Time - networkedInformation.timestampOfMessage));
                newPosition += (rigidbody.velocity * lag);
                rigidbody.MovePosition(newPosition);

                networkedInformation.isDirty = false;
            }
        }
    }
}
