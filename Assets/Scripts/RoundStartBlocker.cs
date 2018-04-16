using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class RoundStartBlocker : MonoBehaviour {
    public float lifeLength = 5f;
    public float collapseTime = .4f;
    public float numberOfParticles;
    public float explosionRadius = 14f;

    EdgeCollider2D edgeCollider;
    LineRenderer lineRenderer;
    Vector3[] originalLinePoints = new Vector3[2];
    float lastResetTime;

    // Use this for initialization
    void Start () {
        edgeCollider = this.EnsureComponent<EdgeCollider2D>();
        lineRenderer = this.EnsureComponent<LineRenderer>();
        lineRenderer.GetPositions(originalLinePoints);

        lastResetTime = Time.time;
    }

    void Update() {
        if (edgeCollider.enabled && (Time.time - lastResetTime) > lifeLength) {
            StartCoroutine(Collapse());
        }
    }

    IEnumerator Collapse() {
        edgeCollider.enabled = false;
        var elapsedTime = 0f;
        var centerPoint = (originalLinePoints[0] + originalLinePoints[1]) / 2;
        while (elapsedTime < collapseTime) {
            lineRenderer.SetPosition(0, Vector3.Lerp(originalLinePoints[0], centerPoint, elapsedTime / collapseTime));
            lineRenderer.SetPosition(1, Vector3.Lerp(originalLinePoints[1], centerPoint, elapsedTime / collapseTime));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        lineRenderer.enabled = false;
        // Reset line to original points
        lineRenderer.SetPositions(originalLinePoints);
        GameObject.Instantiate(GameModel.instance.neutralResources.tronWallSuicidePrefab,
                               transform.position,
                               transform.rotation).EnsureComponent<ParticleSystem>().Play();
    }

    void DisableSelf() {
        if (!edgeCollider.enabled) {
            return;
        }
        edgeCollider.enabled = false;
        lineRenderer.enabled = false;
        var instatiated = GameObject.Instantiate(GameModel.instance.neutralResources.tronWallDestroyedPrefab,
                               transform.position,
                               transform.rotation);
        var ps = instatiated.EnsureComponent<ParticleSystem>();
        var main = ps.main;
        var shape = ps.shape;
        shape.radius = explosionRadius;
        var emission = ps.emission;
        var burst = emission.GetBurst(0);
        burst.count = numberOfParticles;
        emission.SetBurst(0, burst);
        ps.Play();
    }

    public void Reset() {
        edgeCollider.enabled = true;
        lineRenderer.enabled = true;
        lastResetTime = Time.time;
    }

    public void OnCollisionEnter2D(Collision2D collision) {
        var other = collision.gameObject;
        var player = other.GetComponent<Player>();
        var stateManager = other.GetComponent<PlayerStateManager>();

        if (other.GetComponent<Ball>() != null) {
            DisableSelf();
            return;
        }

        if ((player != null) && (stateManager != null) &&
            (stateManager.currentState == State.Dash)) {
            DisableSelf();
            other.EnsureComponent<Rigidbody2D>().velocity = Vector2.zero;
            stateManager.CurrentStateHasFinished();
        }
    }
}
