using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UtilityExtensions;

public class ColorLerpSequence : MonoBehaviour {
    public List<Color> stops;
    public List<float> durations;
    SpriteRenderer renderer;

    void Start() {
        // Debug.LogAssertion(stops.Count >= 2);
        // Debug.LogAssertion(stops.Count == (durations.Count + 1));
        renderer = GetComponent<SpriteRenderer>();
    }

    public bool shouldStart = false;
    void Update() {
        if (shouldStart) {
            StartLerping();
            shouldStart = false;
        }
    }
    
    public void StartLerping() {
        StartCoroutine(Step(0));
    }

    IEnumerator Step(int i) {
        Color startColor = stops[i];
        Color endColor = stops[i+1];
        float duration = durations[i];
        Debug.LogFormat("Lerping from {0} to {1}", startColor, endColor);
        
        float timeElapsed = 0.0f;
        float progress = 0.0f;
        while (timeElapsed < duration) {
            timeElapsed += Time.deltaTime;
            progress = timeElapsed / duration;
            renderer.color = Color.Lerp(startColor, endColor, progress);
            yield return null;
        }
        renderer.color = endColor;

        if (i < stops.Count-2) {
            StartCoroutine(Step(i+1));
        }
    }
    
}
