using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing;

[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundScroller : MonoBehaviour {
    public PostProcessingProfile ppp;
    public float scrollMagnitude = 50.0f;
    public float chromeMagnitude = 0.5f;
    public float rotationRate = 0.0f;
    public float loopTime = 0.0f;
    public float transitionTime = 0.3f;

    private Coroutine zoomer;
    private GameModel gm;
    private new SpriteRenderer renderer;
    private Vector3 origin;
    private float currentChrome = 0.0f;
    private float transformAmount = 0.0f;
    private bool inSlowMo = false;

    void Start() {
        gm = GameObject.Find("GameModel").GetComponent<GameModel>();
        renderer = GetComponent<SpriteRenderer>();
        origin = transform.position;
        currentChrome = ppp.chromaticAberration.settings.intensity;
    }

    void OnDestroy() {
        var settings = ppp.chromaticAberration.settings;
        settings.intensity = 0.0f;
        ppp.chromaticAberration.settings = settings;
    }

    void Update() {
        var x = scrollMagnitude * Mathf.Sin(2 * Mathf.PI * Time.time / loopTime);
        var y = scrollMagnitude * Mathf.Cos(2 * Mathf.PI * Time.time / loopTime);

        // Scale and transform bg according to slowmo state.
        if (gm.InSlowMo() && !inSlowMo) {
            inSlowMo = true;
            if (zoomer != null) StopCoroutine(zoomer);
            zoomer = StartCoroutine(ZoomBG(chromeMagnitude));
        }
        else if (!gm.InSlowMo() && inSlowMo) {
            inSlowMo = false;
            if (zoomer != null) StopCoroutine(zoomer);
            zoomer = StartCoroutine(ZoomBG(0.0f));
        }

        transform.position = origin + new Vector3(x, y, 0);
        transform.Rotate(new Vector3(0, 0, rotationRate * Time.deltaTime));
        var settings = ppp.chromaticAberration.settings;
        settings.intensity = currentChrome;
        ppp.chromaticAberration.settings = settings;
    }

    IEnumerator ZoomBG(float chromeTarget) {
        var chromeStart  = ppp.chromaticAberration.settings.intensity;
        var t            = 0.0f;

        while (currentChrome != chromeTarget && t <= transitionTime) {
            currentChrome = Mathf.SmoothStep(chromeStart, chromeTarget, t / transitionTime);

            t += Time.deltaTime;

            yield return null;
        }

        currentChrome = chromeTarget;

        zoomer = null;
    }

    public void SetBackground(TeamResourceManager resource) {
        renderer.sprite = resource.background;
    }
}
