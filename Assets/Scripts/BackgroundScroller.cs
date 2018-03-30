using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScroller : MonoBehaviour {
    public List<Sprite> backgrounds;
    public float scrollMagnitude = 0.0f;
    public float rotationRate = 0.0f;
    public float loopTime = 0.0f;

    private SpriteRenderer rend;
    private Vector3 origin;
    private int index;

    void Start() {
        rend = GetComponent<SpriteRenderer>();
        origin = transform.position;
        index = DefaultIndex();
    }

    void Update() {
        var x = scrollMagnitude * Mathf.Sin(2 * Mathf.PI * Time.time / loopTime);
        var y = scrollMagnitude * Mathf.Cos(2 * Mathf.PI * Time.time / loopTime);

        transform.position = origin + new Vector3(x, y, 0);
        transform.Rotate(new Vector3(0, 0, rotationRate * Time.deltaTime));
    }

    public int CurrentIndex() {
        return index;
    }

    public int DefaultIndex() {
        return backgrounds.Count / 2;
    }

    public void SetBackground(int i) {
        if (i < 0 || i > backgrounds.Count) return;
        index = i;
        rend.sprite = backgrounds[index];
    }
}
