using Photon.Pun;
using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviour
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
            notificationManager.NotifyMessageWithoutSender(message);
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
