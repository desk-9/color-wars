using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScroller : MonoBehaviour {
    public float scrollMagnitude = 0.0f;
    public float rotationRate = 0.0f;
    public float loopTime = 0.0f;

    private new SpriteRenderer renderer;
    private Vector3 origin;

    void Start() {
        renderer = GetComponent<SpriteRenderer>();
        origin = transform.position;
    }

    void Update() {
        var x = scrollMagnitude * Mathf.Sin(2 * Mathf.PI * Time.time / loopTime);
        var y = scrollMagnitude * Mathf.Cos(2 * Mathf.PI * Time.time / loopTime);

        transform.position = origin + new Vector3(x, y, 0);
        transform.Rotate(new Vector3(0, 0, rotationRate * Time.deltaTime));
    }

    public void SetBackground(TeamResourceManager resource) {
        renderer.sprite = resource.background;
    }
}
