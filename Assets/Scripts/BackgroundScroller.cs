using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScroller : MonoBehaviour {
    public ChromeEffect chrome;
    public float scrollMagnitude = 50.0f;
    public float chromeMagnitude = 0.5f;
    public float rotationRate = 0.0f;
    public float loopTime = 0.0f;
    public float transitionTime = 0.3f;

    private Coroutine zoomer;
    private GameModel gm;
    private new SpriteRenderer renderer;
    private Vector3 origin;
    private float transformAmount = 0.0f;
    private bool inSlowMo = false;

    void Start() {
        gm = GameObject.Find("GameModel").GetComponent<GameModel>();
        renderer = GetComponent<SpriteRenderer>();
        origin = transform.position;
    }

    void Update() {
        var x = scrollMagnitude * Mathf.Sin(2 * Mathf.PI * Time.time / loopTime);
        var y = scrollMagnitude * Mathf.Cos(2 * Mathf.PI * Time.time / loopTime);

        // Scale and transform bg according to slowmo state.
        if (gm.InSlowMo() && !inSlowMo) {
            inSlowMo = true;
            if (zoomer != null) StopCoroutine(zoomer);
            chrome.SetIntensitySmooth(chromeMagnitude, transitionTime);
        }
        else if (!gm.InSlowMo() && inSlowMo) {
            inSlowMo = false;
            chrome.SetIntensitySmooth(0.0f, transitionTime);
        }

        transform.position = origin + new Vector3(x, y, 0);
        transform.Rotate(new Vector3(0, 0, rotationRate * Time.deltaTime));
    }

    public void SetBackground(TeamResourceManager resource) {
        renderer.sprite = resource.background;
    }
}
