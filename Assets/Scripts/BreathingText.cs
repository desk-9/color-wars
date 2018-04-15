using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class BreathingText : MonoBehaviour {

    public bool lerpSize = true;
    public int minFontSize;
    public int maxFontSize;
    public float period;
    public AnimationCurve fontSizeCurve;

    public bool lerpColor = true;
    public Color startColor = Color.clear;
    public Color endColor = Color.clear;
    public AnimationCurve colorCurve;

    Text text;
    ContentSizeFitter contentSizeFitter;

    public void Start() {
        // The ContentSizeFitter component makes the breathing text scale
        // w/aspect ratio
        // See also:
        // https://answers.unity.com/questions/1040610/re-sizing-ui-text-font-relative-to-screen-size.html
        // https://docs.unity3d.com/Manual/HOWTO-UIFitContentSize.html
        contentSizeFitter = this.EnsureComponent<ContentSizeFitter>();
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;        

        // Text lerping seems to look wayyyy better with these settings
        text = this.EnsureComponent<Text>();

        // Makes the text lerping stay "centered" in place
        text.alignByGeometry = true;

        // Allow the text to breathe even if the RectTransform is set to stretch
        text.resizeTextForBestFit = true; // "Best Fit" checkbox in the editor
        text.resizeTextMinSize = minFontSize - 5; // "Best Fit" -> Min Size in editor
        text.resizeTextMaxSize = maxFontSize + 5; // "Best Fit" -> Max Size in editor
        // I'm adding/subtracting 5, because the font size is constrained by those values (and I want to have some wiggle room)
        
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        // Start lerping text size
        if (lerpSize) {
            StartCoroutine(
                TransitionUtility.PingPongFloat(
                    (value) => text.fontSize = (int) value,
                    minFontSize, maxFontSize, period,
                    useGameTime: false, animationCurve: fontSizeCurve));
        }

        // Start lerping text color
        if (lerpColor) {
            StartCoroutine(
                TransitionUtility.PingPongColor(
                    (value) => text.color = value,
                    startColor, endColor, period,
                    useGameTime: false, animationCurve: colorCurve));
        }

    }
        
}
