using System.Collections;
using UnityEngine;
using UtilityExtensions;

public class RoundStartBlocker : MonoBehaviour
{
    public float lifeLength = 5f;
    public float collapseTime = .4f;
    public float numberOfParticles;
    public float explosionRadius = 14f;
    private EdgeCollider2D edgeCollider;
    private LineRenderer lineRenderer;
    private Vector3[] originalLinePoints = new Vector3[2];
    private float lastResetTime;

    // Use this for initialization
    private void Start()
    {
        edgeCollider = this.EnsureComponent<EdgeCollider2D>();
        lineRenderer = this.EnsureComponent<LineRenderer>();
        lineRenderer.GetPositions(originalLinePoints);

        lastResetTime = Time.time;
    }

    private void Update()
    {
        if (edgeCollider.enabled && (Time.time - lastResetTime) > lifeLength)
        {
            StartCoroutine(Collapse());
        }
    }

    private IEnumerator Collapse()
    {
        edgeCollider.enabled = false;
        float elapsedTime = 0f;
        Vector3 centerPoint = (originalLinePoints[0] + originalLinePoints[1]) / 2;
        while (elapsedTime < collapseTime)
        {
            lineRenderer.SetPosition(0, Vector3.Lerp(originalLinePoints[0], centerPoint, elapsedTime / collapseTime));
            lineRenderer.SetPosition(1, Vector3.Lerp(originalLinePoints[1], centerPoint, elapsedTime / collapseTime));
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        lineRenderer.enabled = false;
        // Reset line to original points
        lineRenderer.SetPositions(originalLinePoints);
        GameObject.Instantiate(GameManager.instance.neutralResources.tronWallSuicidePrefab,
                               transform.position,
                               transform.rotation).EnsureComponent<ParticleSystem>().Play();
    }

    private void DisableSelf()
    {
        if (!edgeCollider.enabled)
        {
            return;
        }
        edgeCollider.enabled = false;
        lineRenderer.enabled = false;
        GameObject instatiated = GameObject.Instantiate(GameManager.instance.neutralResources.tronWallDestroyedPrefab,
                               transform.position,
                               transform.rotation);
        ParticleSystem ps = instatiated.EnsureComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.radius = explosionRadius;
        ParticleSystem.EmissionModule emission = ps.emission;
        ParticleSystem.Burst burst = emission.GetBurst(0);
        burst.count = numberOfParticles;
        emission.SetBurst(0, burst);
        ps.Play();
    }

    public void Reset()
    {
        edgeCollider.enabled = true;
        lineRenderer.enabled = true;
        lastResetTime = Time.time;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;
        Player player = other.GetComponent<Player>();
        PlayerStateManager stateManager = other.GetComponent<PlayerStateManager>();

        if (other.GetComponent<Ball>() != null)
        {
            DisableSelf();
            return;
        }

        if ((player != null) && (stateManager != null) &&
            (stateManager.currentState == OldState.Dash))
        {
            DisableSelf();
            other.EnsureComponent<Rigidbody2D>().velocity = Vector2.zero;
            stateManager.CurrentStateHasFinished();
        }
    }
}
