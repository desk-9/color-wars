using System.Collections;
using UnityEngine;
using UtilityExtensions;
using System.Linq;

public class TronWall : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The amount of time that the destroyer of a wall is stunned")]
    private float wallBreakerStunTime = .35f;

    public float wallDestroyTime = .3f;
    public int maxParticlesOnDestroy = 100;
    public float knockbackOnBreak = 1f;

    private float lifeLength { get; set; }

    private TeamManager team;
    private LineRenderer lineRenderer;
    private Vector3[] linePoints = new Vector3[2];
    private PlayerTronMechanic creator;
    private Coroutine stretchWallCoroutine;
    private EdgeCollider2D edgeCollider;
    private float tronWallOffset;

    public void Initialize(PlayerTronMechanic creator, float lifeLength, TeamManager team,
                            float tronWallOffset)
    {
        this.lifeLength = lifeLength;
        this.team = team;
        this.creator = creator;
        this.tronWallOffset = tronWallOffset;

        lineRenderer = this.EnsureComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        linePoints[0] = creator.transform.position - ((creator.transform.position - transform.position)).normalized * tronWallOffset;

        edgeCollider = this.EnsureComponent<EdgeCollider2D>();

        lineRenderer.material = team.resources.wallMaterial;
        stretchWallCoroutine = StartCoroutine(StretchWall());
    }

    private IEnumerator StretchWall()
    {
        while (true)
        {
            Vector3 endPoint = creator.transform.position - ((creator.transform.position - transform.position)).normalized * tronWallOffset;
            linePoints[1] = endPoint;
            SetRendererAndColliderPoints();
            yield return new WaitForFixedUpdate();
        }
    }

    public void PlaceWall()
    {
        if (stretchWallCoroutine != null)
        {
            StopCoroutine(stretchWallCoroutine);
            stretchWallCoroutine = null;
            this.TimeDelayCall(() => StartCoroutine(Collapse()), lifeLength);
        }
    }

    private void SetRendererAndColliderPoints()
    {
        lineRenderer.SetPositions(linePoints);
        edgeCollider.points = linePoints.
            Select(point => (Vector2)transform.InverseTransformPoint(point)).ToArray();
    }

    public void KillSelf()
    {
        if (this == null)
        {
            return;
        }
        AudioManager.instance.BreakWall.Play(.5f);
        PlayDestroyedParticleEffect();
        creator.StopWatching(this);
        Destroy(gameObject);
    }

    private IEnumerator Collapse()
    {
        creator.StopWatching(this);
        float elapsedTime = 0f;
        Vector3 startingPoint = linePoints[0];
        while (elapsedTime < wallDestroyTime)
        {
            linePoints[0] = Vector3.Lerp(startingPoint, linePoints[1], elapsedTime / wallDestroyTime);
            SetRendererAndColliderPoints();
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
        yield break;
    }

    public void PlayDestroyedParticleEffect()
    {
        float magnitude = (linePoints[1] - linePoints[0]).magnitude;
        GameObject instantiated = GameObject.Instantiate(team.resources.tronWallDestroyedPrefab,
                                                  (linePoints[1] + linePoints[0]) / 2, transform.rotation);
        ParticleSystem ps = instantiated.EnsureComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.startColor = team.TeamColor.color;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.radius = magnitude * .65f;
        ParticleSystem.EmissionModule emission = ps.emission;
        ParticleSystem.Burst burst = emission.GetBurst(0);
        burst.count = Mathf.Min(magnitude * burst.count.constant, maxParticlesOnDestroy);
        emission.SetBurst(0, burst);
        ps.Play();
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;
        Player player = other.GetComponent<Player>();

        // If the wall is still being laid
        if (stretchWallCoroutine != null)
        {
            // Check if it was your teammate
            Player otherPlayer = other.GetComponent<Player>();
            if (otherPlayer != null &&
                otherPlayer.Team.TeamColor == team.TeamColor)
            {
                return;
            }

            creator.HandleWallCollision();
            GameManager.Instance.NotificationManager.NotifyMessage(Message.TronWallDestroyedWhileLaying, creator.gameObject);
            PlayDestroyedParticleEffect();
            Destroy(gameObject);
            return;
        }

        Ball ball = other.GetComponent<Ball>();
        if (ball != null)
        {
            KillSelf();
        }
        else if ((player != null) && 
            (player.StateManager != null) &&
            (player.StateManager.CurrentState == State.Dash))
        {
            KillSelf();

            // We knock back left because the sprites are facing right...so left is
            // the back end of the character
            Vector3 knockBackdirection = -player.transform.right;
            player.StateManager.StunNetworked(player.PlayerMovement.CurrentPosition,
                knockBackdirection * knockbackOnBreak, wallBreakerStunTime, false);
            GameManager.Instance.NotificationManager.NotifyMessage(Message.TronWallDestroyed, other);
        }

    }
}
