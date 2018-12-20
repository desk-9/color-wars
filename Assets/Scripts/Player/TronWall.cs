using System.Collections;
using UnityEngine;
using UtilityExtensions;
using System.Linq;

public class TronWall : MonoBehaviour
{

    #region Tweakables
    public float wallCollapseDuration = .3f;
    public int maxParticlesOnDestroy = 100;
    public float wallBreakerKnockback = 1f;
    #endregion

    #region Variables set by `Initialize` -- DO NOT TWEAK HERE
    private PlayerTronMechanic creator;
    private float lifespan { get; set; }
    private TeamManager team;
    private float tronWallOffset;
    #endregion

    #region Misc private member variables
    private LineRenderer lineRenderer;
    private Coroutine stretchWallCoroutine;
    private EdgeCollider2D edgeCollider;
    #endregion

    #region Member variables/methods for the start/end points of line
    private Vector3[] linePoints = new Vector3[2];
    private Vector3 startPoint
    {
        get {return linePoints[0];}
        set
        {
            linePoints[0] = value;
            this.UpdateEndpoints();
        }
    }
    private Vector3 endPoint
    {
        get {return linePoints[1];}
        set
        {
            linePoints[1] = value;
            this.UpdateEndpoints();
        }
    }
    private void UpdateEndpoints()
    {
        lineRenderer.SetPositions(linePoints);
        edgeCollider.points = linePoints.
            Select(point => (Vector2)transform.InverseTransformPoint(point)).ToArray();
    }
    #endregion

    public void BeginConstruction(PlayerTronMechanic creator,
                                  float lifespan,
                                  TeamManager team,
                                  float tronWallOffset)
    {
        this.lifespan = lifespan;
        this.team = team;
        this.creator = creator;
        this.tronWallOffset = tronWallOffset;

        lineRenderer = this.EnsureComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        Vector3 initPoint = creatorLayingOrigin();
        this.linePoints = new Vector3[] {initPoint, initPoint};

        edgeCollider = this.EnsureComponent<EdgeCollider2D>();

        lineRenderer.material = team.resources.wallMaterial;
        stretchWallCoroutine = StartCoroutine(StretchWall());
    }

    private IEnumerator StretchWall()
    {
        while (true)
        {
            this.endPoint = creatorLayingOrigin();
            yield return new WaitForFixedUpdate();
        }
    }

    private Vector3 creatorLayingOrigin() {
        Vector3 offsetDirection = ((creator.transform.position - transform.position)).normalized;
        return creator.transform.position - offsetDirection * tronWallOffset;
    }

    public void FinishConstruction()
    {
        if (stretchWallCoroutine != null)
        {
            StopCoroutine(stretchWallCoroutine);
            stretchWallCoroutine = null;
            this.TimeDelayCall(() => StartCoroutine(CollapseThenRemove()), lifespan);
        }
    }

    // called when the wall's lifespan is over
    // (i.e. it's been `lifespan` seconds since the wall was created)
    private IEnumerator CollapseThenRemove()
    {
        // NOTE: we don't actually call `Remove` here:
        // This is because `Remove` will instantaneously:
        //   1. tells the creator to stop watching the wall
        //   2. destroys this gameobject
        // But we want to do stuff between steps 1 & 2 (i.e. collapse the wall).
        creator.StopWatching(this);
        float elapsedTime = 0f;
        Vector3 oldStartPoint = this.startPoint;
        while (elapsedTime < wallCollapseDuration)
        {
            this.startPoint = Vector3.Lerp(oldStartPoint, this.endPoint, elapsedTime / wallCollapseDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
        yield break;
    }


    // MAYBE TODO / IDEA: Move the collision-related logic out of this component
    // -- maybe add components to Ball and BallCarrier which implement a
    // `WallBreaker` interface?
    //
    // Specifically, I think it'd be cleaner if:
    // 1. the TronWall provides public `BreakWall`-flavored methods (e.g.
    //    CollapseWall, ShatterWall, etc)
    // 2. the WallBreaker components check whether it's done enough to break the wall
    //    (e.g. for the PlayerWallBreaker "Am i on the same team as the wall's
    //    owner? Yes? OK nvm")
    // 3. the WallBreaker only calls TronWall's public methods if the
    //    WallBreaker's wall-breaking conditions have been satisfied
    public void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;
        Player otherPlayer = other.GetComponent<Player>();
        PlayerStateManager otherPlayerStateManager = other.GetComponent<PlayerStateManager>();
        Ball ball = other.GetComponent<Ball>();

        if (stretchWallCoroutine != null)
        {
            // Do nothing if your teammate collides with the wall
            if (otherPlayer != null &&
                otherPlayer.team.color == team.color)
            {
                return;
            }
            HandleWallCollisionWhileLaying();
            return;
        }

        // Case: wall broken by ball
        // (but NOT while the wall was still being laid)
        if (ball != null)
        {
            BreakWallAndRemove();
            return;
        }
        // Case: wall broken by enemy player
        // (but NOT while the wall was still being laid)
        else if ((otherPlayer != null) && (otherPlayerStateManager != null) &&
                 (otherPlayerStateManager.currentState == State.Dash))
        {
            HandleWallCollision(other, otherPlayer, otherPlayerStateManager);
            return;
        }

    }

    #region Helpers for OnCollisionEnter2D
    private void HandleWallCollisionWhileLaying()
    {
        creator.HandleWallCollisionWhileLaying();
        BreakWallAndRemove();
        GameManager.instance.notificationManager.NotifyMessage(
            Message.TronWallDestroyedWhileLaying,
            creator.gameObject);
    }
    private void HandleWallCollision(GameObject other, Player otherPlayer,
                                     PlayerStateManager otherPlayerStateManager)
    {
        BreakWallAndRemove();
        // Stun the player who broke the wall
        PlayerStun otherPlayerStun = other.EnsureComponent<PlayerStun>();
        otherPlayerStateManager.AttemptStun(
            () => {
                Vector3 stunDirection = -1.0f * otherPlayer.transform.right * wallBreakerKnockback;
                other.EnsureComponent<Rigidbody2D>().velocity = Vector2.zero;
                otherPlayerStun.StartStun(stunDirection, creator.wallBreakerStunTime);
                GameManager.instance.notificationManager.NotifyMessage(Message.TronWallDestroyed, other);
            },
            otherPlayerStun.StopStunned);
    }
    #endregion

    #region Helpers related to destruction FX & removal/destruction/cleanup
    public void LruRemove()
    {
        BreakWallAndRemove(shouldPlaySound: false);
    }

    public void BreakWallAndRemove(bool shouldPlaySound=true)
    {
        if (this == null)
        {
            return;
        }
        BreakWallEffects(shouldPlaySound);
        Remove();
    }

    public void BreakWallEffects(bool shouldPlaySound=true) {
        if (shouldPlaySound) {
            AudioManager.instance.BreakWall.Play(.5f);
        }
        PlayWallShatteredParticleEffect();
    }

    public void PlayWallShatteredParticleEffect()
    {
        float magnitude = (this.endPoint - this.startPoint).magnitude;
        GameObject instantiated = GameObject.Instantiate(
            team.resources.tronWallDestroyedPrefab,
            (this.endPoint + this.startPoint) / 2,
            transform.rotation);
        ParticleSystem ps = instantiated.EnsureComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.startColor = team.resources.color;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.radius = magnitude * .65f;
        ParticleSystem.EmissionModule emission = ps.emission;
        ParticleSystem.Burst burst = emission.GetBurst(0);
        burst.count = Mathf.Min(magnitude * burst.count.constant, maxParticlesOnDestroy);
        emission.SetBurst(0, burst);
        ps.Play();
    }

    public void Remove() {
        creator.StopWatching(this);
        Destroy(gameObject);
    }
    #endregion

}
