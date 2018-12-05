using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserGuide : MonoBehaviour
{

    public float drawDistanceAfterCollision = 30.0f;
    public float epsilon = 0.02f;
    private const float RAYCAST_LIMIT = 300f;
    private LineRenderer lineRenderer;
    private Coroutine laserCoroutine;
    private LayerMask rayCastMask;
    private int goalLayer;

    // These are set programmatically by referencing TeamResourceManager
    // => Should *not* be public
    private Gradient aimLaserGradient;
    private Gradient aimLaserToGoalGradient;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
        rayCastMask = LayerMask.GetMask(new string[] { "Wall", "Goal", "TronWall" });
        goalLayer = LayerMask.NameToLayer("Goal");

        // Default for the team select screen
        aimLaserGradient = new Gradient();
        aimLaserGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.8f),
                new GradientAlphaKey(0f, 1.0f) }
            );
        aimLaserToGoalGradient = new Gradient();
        aimLaserToGoalGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0.0f),
                new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.8f),
                new GradientAlphaKey(0f, 1.0f) }
            );
    }

    public void SetLaserGradients()
    {
        TeamManager team = GetComponent<Player>()?.team;
        if (team != null)
        {
            aimLaserGradient = team.resources.aimLaserGradient;
            aimLaserToGoalGradient = team.resources.aimLaserToGoalGradient;
        }
    }


    public void StartDrawingLaser()
    {
        lineRenderer.enabled = true;
        laserCoroutine = StartCoroutine(DrawLaser());
    }

    public void StopDrawingLaser()
    {
        lineRenderer.enabled = false;
        if (laserCoroutine != null)
        {
            StopCoroutine(laserCoroutine);
            laserCoroutine = null;
        }
    }

    // Returns true if the laser was reflected, false otherwise
    private IEnumerator DrawLaser()
    {
        yield return null;
        yield return null;
        while (true)
        {
            List<Vector3> points = new List<Vector3>();
            points.Add(transform.position);
            Vector3 laserStart = transform.position;
            Vector3 laserDirection = transform.right;
            float drawDistanceRemaining = drawDistanceAfterCollision;

            RaycastHit2D raycastHit = Physics2D.Raycast(
                laserStart + laserDirection * epsilon,
                laserDirection,
                RAYCAST_LIMIT,
                rayCastMask);

            Debug.Assert(raycastHit.collider != null, "Make RAYCAST_LIMIT larger");
            points.Add(raycastHit.point);

            lineRenderer.colorGradient = aimLaserGradient;

            // terminate laser if it hits the goal on the first reflection
            if (raycastHit.transform.gameObject.layer != goalLayer)
            {
                laserStart = raycastHit.point;
                laserDirection = Vector3.Reflect(laserDirection, raycastHit.normal);

                while (drawDistanceRemaining > 0f)
                {
                    // try getting another ricochet point
                    raycastHit = Physics2D.Raycast(
                        laserStart + laserDirection * epsilon,
                        laserDirection,
                        drawDistanceRemaining,
                        rayCastMask);

                    // Case: raycast hit something
                    if (raycastHit.collider != null)
                    {
                        // Add the line segment to ricochet point
                        points.Add(raycastHit.point);
                        if (raycastHit.transform.gameObject.layer == goalLayer)
                        {
                            break;
                        }
                        drawDistanceRemaining -= ((Vector2)laserStart - raycastHit.point).magnitude;
                        laserStart = raycastHit.point;
                        laserDirection = Vector3.Reflect(laserDirection, raycastHit.normal);
                    }
                    // Case: raycast didn't hit anything
                    // (probably not enough `drawDistanceRemaining`)
                    else
                    {
                        // Use up the `drawDistanceRemaining` in the right direction
                        points.Add(laserStart + laserDirection * drawDistanceRemaining);
                        drawDistanceRemaining = 0f;
                    }
                }
            }

            // Case: We're pointing right at the goal (no reflections)
            // => set special laser gradient color
            else
            {
                lineRenderer.colorGradient = aimLaserToGoalGradient;
            }

            Vector3[] pointsArray = points.ToArray();
            if (lineRenderer.positionCount != pointsArray.Length)
            {
                lineRenderer.positionCount = pointsArray.Length;
            }

            lineRenderer.SetPositions(pointsArray);
            yield return null;
        }
    }
}
