using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class BreathingText : MonoBehaviour {

    public bool lerpFontSize = true;
    // public int minFontSize;
    // public int maxFontSize;
    public float period;
    public AnimationCurve fontSizeCurve;

    public bool lerpColor = true;
    public Color startColor = Color.clear;
    public Color endColor = Color.clear;
    public AnimationCurve colorCurve;

    Text text;

    RectTransform rect;
    public bool lerpRectTransform = true;
    // REMARK: both x & y scale are set by the same factor in this code, so
    // there's only a min/max scale rather than a min/max y scale & a min/max x
    // scale. This is to prevent stretching/weird warping of canvas elements
    public float minRectScale;
    public float maxRectScale;

    public void Start() {

        rect = this.EnsureComponent<RectTransform>();
        
        // Text lerping seems to look wayyyy better with these settings
        text = this.EnsureComponent<Text>();
        InitializeTextElement();

        if (lerpColor) {
            LerpColor();
        }

        if (lerpFontSize) {
            LerpFontSize();
        }

    }

    void InitializeTextElement() {
        // Makes the text lerping stay "centered" in place
        text.alignByGeometry = true;

        // Make sure that the "best fit" fontSize will scale nicely
        text.resizeTextForBestFit = true; // "Best Fit" checkbox in the editor
        text.resizeTextMinSize = text.fontSize - 5; // "Best Fit" -> Min Size in editor
        text.resizeTextMaxSize = text.fontSize + 5; // "Best Fit" -> Max Size in editor
        // I'm adding/subtracting 5, because the font size is constrained by those values (and I want to have some wiggle room)

        // Make sure that the text isn't truncated / wrapped (i.e. invisible)
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
    }

    void LerpColor() {
        StartCoroutine(
            TransitionUtility.PingPongColor(
                (value) => text.color = value,
                startColor, endColor, period,
                useGameTime: false, animationCurve: colorCurve));
    }

    void LerpFontSize() {
        StartCoroutine(
            TransitionUtility.PingPongFloat(
                (newScale) => rect.localScale = new Vector3(newScale, newScale, 1.0f),
                minRectScale, maxRectScale, period,
                useGameTime: false, animationCurve: fontSizeCurve));
    }

}
