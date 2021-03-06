using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TransitionUtility : MonoBehaviour
{
    // Class for utility functions involving screen transitions, color lerping

    // ASSUMPTION: All animation curves for this file have values in the range
    // [0,1], and the actual max/min values to attain are passed as parameters
    // to any lerpy functions
    public static TransitionUtility instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public static IEnumerator PingPongFloat(FloatSetter floatSetter,
                                            float minValue, float maxValue,
                                            float period, bool useGameTime = false,
                                            AnimationCurve animationCurve = null)
    {

        animationCurve = animationCurve ?? AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
        animationCurve.preWrapMode = WrapMode.PingPong;
        animationCurve.postWrapMode = WrapMode.PingPong;
        float startTime = Time.realtimeSinceStartup;
        float timeElapsed = 0.0f;
        float progress = 0.0f;
        floatSetter(minValue);
        float duration = period / 2;
        while (true)
        {
            timeElapsed = UpdateTimeElapsed(timeElapsed, startTime, useGameTime);
            progress = timeElapsed / duration;
            float scaledProgress = ScaleProgress(
                progress, minValue, maxValue, animationCurve);
            floatSetter(scaledProgress);
            yield return null;
        }
    }
    public static IEnumerator PingPongColor(ColorSetter colorSetter,
                                            Color startValue, Color endValue,
                                            float period, bool useGameTime = false,
                                            AnimationCurve animationCurve = null)
    {

        animationCurve = animationCurve ?? AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);
        animationCurve.preWrapMode = WrapMode.PingPong;
        animationCurve.postWrapMode = WrapMode.PingPong;
        float startTime = Time.realtimeSinceStartup;
        float timeElapsed = 0.0f;
        float progress = 0.0f;
        colorSetter(startValue);
        float duration = period / 2;
        while (true)
        {
            timeElapsed = UpdateTimeElapsed(timeElapsed, startTime, useGameTime);
            progress = timeElapsed / duration;
            float scaledProgress = ScaleProgress(progress, 0.0f, 1.0f, animationCurve);
            Color newColor = Color.Lerp(startValue, endValue, scaledProgress);
            colorSetter(newColor);
            yield return null;
        }
    }

    private static float UpdateTimeElapsed(float timeElapsed, float startTime,
                                   bool useGameTime = false)
    {
        if (useGameTime) { return timeElapsed + Time.deltaTime; }
        return Time.realtimeSinceStartup - startTime;
    }

    public static IEnumerator LerpFloat(FloatSetter floatSetter,
                                        float startValue, float endValue,
                                        float duration, bool useGameTime = false,
                                        AnimationCurve animationCurve = null)
    {

        float startTime = Time.realtimeSinceStartup;
        float timeElapsed = 0.0f;
        float progress = 0.0f;
        floatSetter(startValue);
        while (timeElapsed < duration)
        {
            timeElapsed = UpdateTimeElapsed(timeElapsed, startTime, useGameTime);
            progress = timeElapsed / duration;
            float scaledProgress = ScaleProgress(
                progress, startValue, endValue, animationCurve);
            floatSetter(scaledProgress);
            yield return null;
        }
        floatSetter(endValue);
    }

    public static float ScaleProgress(float progress, float startValue, float endValue,
                                      AnimationCurve animationCurve = null)
    {
        float scaledProgress = progress;
        if (animationCurve != null)
        {
            float delta = endValue - startValue;
            scaledProgress = startValue + delta * animationCurve.Evaluate(progress);
        }
        else
        {
            scaledProgress = Mathf.Lerp(startValue, endValue, progress);
        }
        return scaledProgress;
    }

    public static IEnumerator LerpColor(ColorSetter colorSetter,
                                        Color startColor, Color endColor,
                                        float duration, bool useGameTime = false)
    {

        float startTime = Time.realtimeSinceStartup;
        float timeElapsed = 0.0f;
        float progress = 0.0f;
        colorSetter(startColor);
        while (timeElapsed < duration)
        {
            timeElapsed = UpdateTimeElapsed(timeElapsed, startTime, useGameTime);
            progress = timeElapsed / duration;
            Color newColor = Color.Lerp(startColor, endColor, progress);
            colorSetter(newColor);
            yield return null;
        }
        colorSetter(endColor);
    }

    public static IEnumerator LerpColorSequence(ColorSetter colorSetter,
                                                List<Color> stops,
                                                List<float> durations)
    {
        int i = 0;
        while (i < stops.Count - 1)
        {
            Color startColor = stops[i];
            Color endColor = stops[i + 1];
            float duration = durations[i];
            yield return TransitionUtility.instance.StartCoroutine(
                LerpColor(colorSetter, startColor, endColor, duration));
            ++i;
        }
    }

    public static IEnumerator LerpAlpha(ColorSetter colorSetter,
                                        float startOpacity, float endOpacity,
                                        float duration, bool useGameTime = false,
                                        Color? maybeTint = null)
    {
        Color tint = maybeTint ?? Color.black;
        Color startColor = new Color(tint.r, tint.g, tint.b, startOpacity);
        Color endColor = new Color(tint.r, tint.g, tint.b, endOpacity);
        yield return TransitionUtility.instance.StartCoroutine(
            LerpColor(colorSetter, startColor, endColor, duration, useGameTime));
    }

    public class Panel
    {
        public GameObject panel;
        public Image image;
        public Color tint;
        public float duration;
        public float epsilon = 0.05f;
        public Color color
        {
            get { return (image != null) ? image.color : Color.clear; }
            set { image.color = value; }
        }

        public Panel(GameObject panel, float duration = 1.0f)
        {
            this.panel = panel;
            this.image = panel.GetComponent<Image>();
            this.tint = this.color;
            this.duration = duration;
            Hide();
        }

        public static implicit operator Panel(Color color)
        {
            GameObject newPanel = CreateCanvasPanel();
            newPanel.AddComponent<Image>();
            Panel panel = new Panel(newPanel);
            panel.color = color;
            return panel;
        }

        public void Hide() { panel.SetActive(false); }
        public void MakeTransparent()
        {
            Show();
            color = Color.clear;
        }
        public void Show() { panel.SetActive(true); }

        private static GameObject CreateCanvasPanel()
        {
            GameObject newCanvasObj = new GameObject("Canvas");
            Canvas newCanvas = newCanvasObj.AddComponent<Canvas>();
            newCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            newCanvasObj.AddComponent<CanvasScaler>();
            newCanvasObj.AddComponent<GraphicRaycaster>();
            GameObject panel = new GameObject("Panel");
            panel.AddComponent<CanvasRenderer>();
            panel.transform.SetParent(newCanvasObj.transform, false);
            RectTransform rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            return panel;
        }

        public IEnumerator FadeIn()
        {
            panel.SetActive(true);
            yield return LerpAlpha((Color newColor) => color = newColor,
                                   0.0f, tint.a, duration, false, tint);
        }

        public IEnumerator FadeOut()
        {
            panel.SetActive(true);
            yield return CoroutineUtility.RunThenCallback(
                LerpAlpha((Color newColor) => this.color = newColor,
                          tint.a, 0.0f, duration, false, tint),
                Hide);
        }

    }

    public class ScreenTransition
    {
        public Panel panel;

        public ScreenTransition(float duration = 1.0f, Color? maybeColor = null)
        {
            Color color = maybeColor ?? Color.black;
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
        public IEnumerator FadeIn() { yield return panel.FadeOut(); }
        public IEnumerator FadeOut() { yield return panel.FadeIn(); }
    }

    public static void OneShotFadeTransition(float totalDuration,
                                             float blackScreenPauseDuration = 0.0f)
    {
        float fadeDuration = totalDuration * 0.5f;
        ScreenTransition screenTransition = new ScreenTransition(fadeDuration);
        IEnumerator coroutineSequence = CoroutineUtility.RunSequentially(
                screenTransition.FadeOut(),
                CoroutineUtility.WaitForRealtimeSeconds(blackScreenPauseDuration),
                screenTransition.FadeIn()
            );
        TransitionUtility.instance.StartCoroutine(
            CoroutineUtility.RunThenCallback(
                coroutineSequence,
                () => Destroy(screenTransition.panel.panel)));
    }

}
