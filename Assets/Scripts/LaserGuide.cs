using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserGuide : MonoBehaviour {

    public float maxDrawDistance = 50.0f;
    public string WallLayer = "Wall";
    public float epsilon = 0.02f;

    LineRenderer lineRenderer;
    Coroutine laserCoroutine;

    void Start() {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
    }


    public void DrawLaser() {
        lineRenderer.enabled = true;
        laserCoroutine = StartCoroutine(DrawAimingLaser());
    }

    public void StopDrawingLaser() {
        lineRenderer.enabled = false;
        if (laserCoroutine != null) {
            StopCoroutine(laserCoroutine);
            laserCoroutine = null;
        }
    }

    // Returns true if the laser was reflected, false otherwise
    IEnumerator DrawAimingLaser() {
        while (true) {
            var points = new List<Vector3>();
            points.Add(transform.position);
            var laserStart = transform.position;
            var laserDirection = transform.right;
            var drawDistanceRemaining = maxDrawDistance;
            while (drawDistanceRemaining > 0f) {
                var raycastHit = Physics2D.Raycast(laserStart + laserDirection * epsilon,
                                                   laserDirection,
                                                   drawDistanceRemaining,
                                                   LayerMask.GetMask(WallLayer));

                if (raycastHit.collider != null) {
                    points.Add(raycastHit.point);
                    drawDistanceRemaining -= ((Vector2)laserStart - raycastHit.point).magnitude;
                    laserStart = raycastHit.point;
                    laserDirection = Vector3.Reflect(laserDirection, raycastHit.normal);
                } else {
                    points.Add(laserStart + laserDirection * drawDistanceRemaining);
                    drawDistanceRemaining = 0f;
                }

            }

            var pointsArray = points.ToArray();
            if (lineRenderer.positionCount != pointsArray.Length) {
                lineRenderer.positionCount = pointsArray.Length;
            }

            lineRenderer.SetPositions(pointsArray);
            yield return null;
        }
    }
}
