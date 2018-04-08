using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class TransitionUtility : MonoBehaviour {
    // Class for utility functions involving screen transitions, color lerping

    public static TransitionUtility instance;

    void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(gameObject);
        }
    }
    
    public static IEnumerator LerpFloat(FloatSetter floatSetter,
                                        float startValue, float endValue,
                                        float duration, bool useGameTime=false) {

        float startTime = Time.realtimeSinceStartup;
        float timeElapsed = 0.0f;
        float progress = 0.0f;
        floatSetter(startValue);
        while (timeElapsed < duration) {
            if (useGameTime) {
                timeElapsed += Time.deltaTime;
            } else {
                timeElapsed = Time.realtimeSinceStartup - startTime;
            }
            progress = timeElapsed / duration;
            float newFloat = Mathf.Lerp(startValue, endValue, progress);
            floatSetter(newFloat);
            yield return null;
        }
        floatSetter(endValue);
    }

    public static IEnumerator LerpColor(ColorSetter colorSetter,
                                        Color startColor, Color endColor,
                                        float duration, bool useGameTime=false) {

        float startTime = Time.realtimeSinceStartup;
        float timeElapsed = 0.0f;
        float progress = 0.0f;
        colorSetter(startColor);
        while (timeElapsed < duration) {
            if (useGameTime) {
                timeElapsed += Time.deltaTime;
            } else {
                timeElapsed = Time.realtimeSinceStartup - startTime;
            }
            progress = timeElapsed / duration;
            Color newColor = Color.Lerp(startColor, endColor, progress);
            colorSetter(newColor);
            yield return null;
        }
        colorSetter(endColor);
    }

    public static IEnumerator LerpColorSequence(ColorSetter colorSetter,
                                                List<Color> stops,
                                                List<float> durations) {
        int i = 0;
        while (i < stops.Count - 1) {
            Color startColor = stops[i];
            Color endColor = stops[i+1];
            float duration = durations[i];
            yield return TransitionUtility.instance.StartCoroutine(
                LerpColor(colorSetter, startColor, endColor, duration));
            ++i;
        }
    }

    public static IEnumerator LerpAlpha(ColorSetter colorSetter, 
                                        float startOpacity, float endOpacity,
                                        float duration, bool useGameTime=false,
                                        Color? maybeTint=null) {
        Color tint = maybeTint?? Color.black;
        Color startColor = new Color(tint.r, tint.g, tint.b, startOpacity);
        Color endColor = new Color(tint.r, tint.g, tint.b, endOpacity);
        yield return TransitionUtility.instance.StartCoroutine(
            LerpColor(colorSetter, startColor, endColor, duration, useGameTime));
    }

    public class Panel {
        public GameObject panel;
        public Image image;
        public Color tint;
        public float duration;
        public float epsilon = 0.05f;
        public Color color {
            get {return (image != null) ? image.color : Color.clear;}
            set {image.color = value;}
        }

        public Panel(GameObject panel, float duration=1.0f) {
            this.panel = panel;
            this.image = panel.GetComponent<Image>();
            this.tint = this.color;
            this.duration = duration;
            Hide();
        }

        public static implicit operator Panel(Color color) {
            var newPanel = CreateCanvasPanel();
            Image panelImage = newPanel.AddComponent<Image>();
            Panel panel = new Panel(newPanel);
            panel.color = color;
            return panel;
        }

        public void Hide() {panel.SetActive(false);}
        public void MakeTransparent() {
            Show();
            color = Color.clear;
        }
        public void Show() {panel.SetActive(true);}

        static GameObject CreateCanvasPanel() {
            var newCanvasObj = new GameObject("Canvas");
            Canvas newCanvas = newCanvasObj.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvasObj.AddComponent<CanvasScaler>();
            newCanvasObj.AddComponent<GraphicRaycaster>();
            GameObject panel = new GameObject("Panel");
            panel.AddComponent<CanvasRenderer>();
            panel.transform.SetParent(newCanvasObj.transform, false);
            var rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            return panel;
        }

        public IEnumerator FadeIn() {
            panel.SetActive(true);
            yield return LerpAlpha((Color newColor) => color=newColor,
                                   0.0f, tint.a, duration, false, tint);
        }

        public IEnumerator FadeOut() {
            panel.SetActive(true);
            yield return CoroutineUtility.RunThenCallback(
                LerpAlpha((Color newColor) => this.color=newColor,
                          tint.a, 0.0f, duration, false, tint),
                Hide);
        }
        
    }

    public class ScreenTransition {
        public Panel panel;

        public ScreenTransition(float duration=1.0f, Color? maybeColor = null) {
            Color color = maybeColor?? Color.black;
            panel = color;
            panel.tint = color;
            panel.duration = duration;
            panel.Hide();
        }

        // Usually in the context of screen transitions, you want "fade in" to
        // mean "start with opaque black, then reveal scene behind the black
        // panel"
        //
        // For screen transitions, "fade IN" is actually implemented as making a
        // black panel, and then making the black panel fade *OUT*
        public IEnumerator FadeIn() {yield return panel.FadeOut();}
        public IEnumerator FadeOut() {yield return panel.FadeIn();}
    }
    
}
