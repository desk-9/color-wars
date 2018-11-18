using System.Collections;
using UnityEngine;
using UnityEngine.PostProcessing;

[RequireComponent(typeof(PostProcessingProfile))]
public class ChromaticAberrationController : MonoBehaviour
{
    private PostProcessingProfile profile;
    private Coroutine smoothTransition;

    private void Awake()
    {
        profile = GetComponent<PostProcessingBehaviour>().profile;
    }

    private void OnDestroy()
    {
        SetIntensity(0.0f);
    }

    public float GetIntensity()
    {
        return profile.chromaticAberration.settings.intensity;
    }

    public void SetIntensity(float intensity)
    {
        ChromaticAberrationModel.Settings settings = profile.chromaticAberration.settings;
        settings.intensity = intensity;
        profile.chromaticAberration.settings = settings;
    }

    public void SetIntensitySmooth(float target, float time)
    {
        if (smoothTransition != null) StopCoroutine(smoothTransition);

        smoothTransition = StartCoroutine(SmoothTransition(target, time));
    }

    private IEnumerator SmoothTransition(float target, float time)
    {
        float chromeStart = GetIntensity();
        float t = 0.0f;

        while (GetIntensity() != target && t <= time)
        {
            SetIntensity(Mathf.SmoothStep(chromeStart, target, t / time));

            t += Time.deltaTime;

            yield return null;
        }

        smoothTransition = null;
    }
}
