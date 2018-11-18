using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class BreathingText : MonoBehaviour
{

    public float period = 1.0f;

    public bool lerpFontSize = true;
    // REMARK: both x & y scale are set by the same factor in this code, so
    // there's only a min/max rect scale rather than a separate min/max for both
    // x and y. This is to prevent stretching/weird warping of canvas elements
    public float minRectScale = 1.0f;
    public float maxRectScale = 1.15f;
    public AnimationCurve fontSizeCurve = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 1.15f);

    public bool lerpColor = false;
    public Color startColor;
    public Color endColor;
    public AnimationCurve colorCurve;
    private Text text;
    private RichText richText;
    private RectTransform rect;

    public void Start()
    {

        rect = this.EnsureComponent<RectTransform>();

        // Text lerping seems to look wayyyy better with these settings
        text = GetComponent<Text>();
        richText = GetComponent<RichText>();
        InitializeTextElement();

        if (lerpColor)
        {
            LerpColor();
        }

        if (lerpFontSize)
        {
            LerpFontSize();
        }

    }

    private void InitializeTextElement()
    {
        if (text != null)
        {
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
    }

    private void LerpColor()
    {
        StartCoroutine(
            TransitionUtility.PingPongColor(
                (value) =>
                {
                    if (text != null)
                    {
                        text.color = value;
                    }
                    else if (richText != null)
                    {
                        richText.color = value;
                    }
                },
                startColor, endColor, period,
                useGameTime: false, animationCurve: colorCurve));
    }

    private void LerpFontSize()
    {
        StartCoroutine(
            TransitionUtility.PingPongFloat(
                (newScale) => rect.localScale = new Vector3(newScale, newScale, 1.0f),
                minRectScale, maxRectScale, period,
                useGameTime: false, animationCurve: fontSizeCurve));
    }

}
