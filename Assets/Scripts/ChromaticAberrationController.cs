using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PostProcessing;

[RequireComponent(typeof(PostProcessingProfile))]
public class ChromaticAberrationController : MonoBehaviour {
    PostProcessingProfile profile;
    Coroutine smoothTransition;
    float currentIntensity = 0.0f;

    void Awake() {
        profile = GetComponent<PostProcessingBehaviour>().profile;
    }

    void Start() {
        currentIntensity = GetIntensity();
    }

    void OnDestroy() {
        SetIntensity(0.0f);
    }

    public float GetIntensity() {
        return profile.chromaticAberration.settings.intensity;
    }

    public void SetIntensity(float intensity) {
        var settings = profile.chromaticAberration.settings;
        settings.intensity = intensity;
        profile.chromaticAberration.settings = settings;
    }

    public void SetIntensitySmooth(float target, float time) {
        if (smoothTransition != null) StopCoroutine(smoothTransition);

        smoothTransition = StartCoroutine(SmoothTransition(target, time));
    }

    IEnumerator SmoothTransition(float target, float time) {
        var chromeStart  = GetIntensity();
        var t            = 0.0f;

        while (GetIntensity() != target && t <= time) {
            SetIntensity(Mathf.SmoothStep(chromeStart, target, t / time));

            t += Time.deltaTime;

            yield return null;
        }

        currentIntensity = target;
        smoothTransition = null;
    }
}
