using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class CountdownToStart : MonoBehaviour
{

    // Checkbox allows you to manually start a countdown in the editor
    // Purely for testing
    public bool startCountdown = false;

    public float countDuration = 1.0f;
    public float totalDuration
    {
        get
        {
            // subtract 1 here because players can move *on* "GO" rather than
            // *after* "GO"
            return countDuration * (flashText.Count - 1);
        }
    }

    // Alpha values should be in the range [0, 1]
    public float maxPanelAlpha = 0.3f;
    public float minPanelAlpha = 0.0f;
    private float panelAlphaRange;

    public float goTextSizeMultiplier = 1.30f;
    public float maxTextSize = 150.0f;
    public float minTextSize = 24.0f;
    private float textSizeRange;

    // Curve values should be in the range [0, 1]
    public AnimationCurve panelAlpha;
    public AnimationCurve textSize;

    public Text countdownText;
    private Image panelBackground;
    private List<string> flashText = new List<string> { "3", "2", "1", "GO!" };

    private void Start()
    {
        panelBackground = this.EnsureComponent<Image>();
        HideCountdown();

        panelAlphaRange = maxPanelAlpha - minPanelAlpha;
        textSizeRange = maxTextSize - minTextSize;


        GameManager.NotificationManager.CallOnMessage(Message.StartCountdown,
                                            () => StartCountdown());
        if (!PlayerTutorial.runTutorial)
        {
            this.FrameDelayCall(() => StartCountdown(), 3);
        }
    }

    // Checks the startCountdown checkbox (this allows you to manually start a
    // countdown from the editor)
    private void FixedUpdate()
    {
        if (startCountdown == true)
        {
            startCountdown = false;
            StartCountdown();
        }
    }

    public void StartCountdown()
    {
        if (GameManager.Instance.gameOver)
        {
            return;
        }
        StartCoroutine(FlashCount());
    }

    private IEnumerator FlashCount()
    {
        int index = 0;
        float elapsedTime = 0.0f;
        float progress = 0.0f;

        // up to count-1 b/c we do something different on last round
        while (index < flashText.Count - 1)
        {
            elapsedTime = progress = 0.0f;
            countdownText.text = flashText[index];
            while (elapsedTime < countDuration)
            {
                FlashText(progress);
                FlashAlpha(progress);
                elapsedTime += Time.deltaTime;
                progress = elapsedTime / countDuration;
                yield return null;
            }
            ++index;
        }

        // Last round ("GO!" + players can start moving)
        // Do some extra stuff (make text bigger, notify players that they can move)
        textSizeRange *= goTextSizeMultiplier;
        GameManager.NotificationManager.NotifyMessage(Message.CountdownFinished, this);

        // Reset vars for the last count
        elapsedTime = progress = 0.0f;
        countdownText.text = flashText[index];
        while (elapsedTime < countDuration)
        {
            FlashText(progress);
            elapsedTime += Time.deltaTime;
            progress = elapsedTime / countDuration;
            yield return null;
        }
        textSizeRange /= goTextSizeMultiplier;

        HideCountdown();
    }

    private void FlashText(float progress)
    {
        float newTextSize = ScaleByCurve(
            textSize.Evaluate(progress),
            minTextSize, textSizeRange);
        countdownText.fontSize = (int)newTextSize;
    }

    private void FlashAlpha(float progress)
    {
        float newAlpha = ScaleByCurve(
            panelAlpha.Evaluate(progress),
            minPanelAlpha, panelAlphaRange);
        SetPanelAlpha(newAlpha);
    }

    private void HideCountdown()
    {
        countdownText.text = "";
        SetPanelAlpha(0.0f);
    }

    private float ScaleByCurve(float curveValue, float min, float range)
    {
        return min + curveValue * range;
    }

    private void SetPanelAlpha(float newAlpha)
    {
        Color newColor = panelBackground.color;
        newColor.a = newAlpha;
        panelBackground.color = newColor;
    }

}
