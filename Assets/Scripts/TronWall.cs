using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;
using System.Linq;

public class TronWall : MonoBehaviour {

    public float wallDestroyTime = .3f;
    public int maxParticlesOnDestroy = 100;
    public float knockbackOnBreak = 1f;

    float lifeLength {get; set;}
    TeamManager team;
    LineRenderer lineRenderer;
    Vector3[] linePoints = new Vector3[2];
    PlayerTronMechanic creator;
    Coroutine stretchWallCoroutine;
    EdgeCollider2D edgeCollider;
    float tronWallOffset;

    public void Initialize (PlayerTronMechanic creator, float lifeLength, TeamManager team,
                            float tronWallOffset) {
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

    IEnumerator StretchWall() {
        while (true) {
            var endPoint = creator.transform.position - ((creator.transform.position - transform.position)).normalized * tronWallOffset;
            linePoints[1] = endPoint;
            SetRendererAndColliderPoints();
            yield return new WaitForFixedUpdate();
        }
    }

    public void PlaceWall() {
        if (stretchWallCoroutine != null) {
            StopCoroutine(stretchWallCoroutine);
            stretchWallCoroutine = null;
            this.TimeDelayCall(() => StartCoroutine(Collapse()), lifeLength);
        }
    }

    void SetRendererAndColliderPoints() {
        lineRenderer.SetPositions(linePoints);
        edgeCollider.points = linePoints.
            Select(point => (Vector2) transform.InverseTransformPoint(point)).ToArray();
    }

    public void KillSelf() {
        if (this == null) {
            return;
        }
        AudioManager.instance.BreakWall.Play(.5f);
        PlayDestroyedParticleEffect();
        creator.StopWatching(this);
        Destroy(gameObject);
    }

    IEnumerator Collapse() {
        creator.StopWatching(this);
        var elapsedTime = 0f;
        var startingPoint = linePoints[0];
        while (elapsedTime < wallDestroyTime) {
            linePoints[0] = Vector3.Lerp(startingPoint, linePoints[1], elapsedTime / wallDestroyTime);
            SetRendererAndColliderPoints();
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
        yield break;
    }

    public void PlayDestroyedParticleEffect() {
        var magnitude = (linePoints[1] - linePoints[0]).magnitude;
        var instantiated = GameObject.Instantiate(team.resources.tronWallDestroyedPrefab,
                                                  (linePoints[1] + linePoints[0]) / 2, transform.rotation);
        var ps = instantiated.EnsureComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = team.teamColor.color;
        var shape = ps.shape;
        shape.radius = magnitude * .65f;
        var emission = ps.emission;
        var burst = emission.GetBurst(0);
        burst.count = Mathf.Min(magnitude * burst.count.constant, maxParticlesOnDestroy);
        emission.SetBurst(0, burst);
        ps.Play();
    }

    public void OnCollisionEnter2D(Collision2D collision) {
        var other = collision.gameObject;
        var player = other.GetComponent<Player>();
        var stateManager = other.GetComponent<PlayerStateManager>();

        if (stretchWallCoroutine != null) {
            // Check if it was your teammate
            var otherPlayer = other.GetComponent<Player>();
            if (otherPlayer != null &&
                otherPlayer.team.teamColor == team.teamColor) {
                return;
            }

            creator.HandleWallCollision();
            GameModel.instance.nc.NotifyMessage(Message.TronWallDestroyedWhileLaying, creator.gameObject);
            PlayDestroyedParticleEffect();
            Destroy(gameObject);
            return;
        }

        var ball = other.GetComponent<Ball>();
        if (ball != null) {
            KillSelf();
        } else if ((player != null) && (stateManager != null) &&
            (stateManager.currentState == State.Dash)) {
            KillSelf();
            var playerStun = other.EnsureComponent<PlayerStun>();
            stateManager.AttemptStun(() =>
                    { var otherDirection = player.transform.right;
                      other.EnsureComponent<Rigidbody2D>().velocity = Vector2.zero;
                      playerStun.StartStun(-otherDirection * knockbackOnBreak, creator.wallBreakerStunTime);
                      GameModel.instance.nc.NotifyMessage(Message.TronWallDestroyed, other);
                    },
                                     playerStun.StopStunned);
        }

    }
}
