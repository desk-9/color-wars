using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class CountdownToStart : MonoBehaviour {

    // Checkbox allows you to manually start a countdown in the editor
    // Purely for testing
    public bool startCountdown = false;
    
    public float countDuration = 1.0f;
    public float totalDuration {
        get {
            return countDuration * flashText.Count;
        }
    }

    // Alpha values should be in the range [0, 1]
    public float maxPanelAlpha = 0.3f;
    public float minPanelAlpha = 0.0f;
    float panelAlphaRange;

    public float maxTextSize = 150.0f;
    public float minTextSize = 24.0f;
    float textSizeRange;

    // Curve values should be in the range [0, 1]
    public AnimationCurve panelAlpha;
    public AnimationCurve textSize;
    
    int maxCount = 3; // inclusive
    int minCount = 1; // inclusive
    
    public Text countdownText;
    Image panelBackground;
    List<string> flashText = new List<string>{ "3", "2", "1", "GO!"};

    void Start() {
        panelBackground = this.EnsureComponent<Image>();
        HideCountdown();

        panelAlphaRange = maxPanelAlpha - minPanelAlpha;
        textSizeRange = maxTextSize - minTextSize;

        
        GameModel.instance.nc.CallOnMessage(Message.StartCountdown,
                                            () => StartCountdown());
        this.FrameDelayCall(() => StartCountdown(), 3);
    }

    // Checks the startCountdown checkbox (this allows you to manually start a
    // countdown from the editor)
    void FixedUpdate() {
        if (startCountdown == true) {
            startCountdown = false;
            StartCountdown();
        }
    }
    
    public void StartCountdown() {
        if (GameModel.instance.gameOver) {
            return;
        }
        StartCoroutine(FlashCount());
    }

    IEnumerator FlashCount() {
        int index = 0;
        while (index < flashText.Count) {
            countdownText.text = flashText[index];

            float elapsedTime = 0.0f;
            float progress = 0.0f;
            while (elapsedTime < countDuration) {

                float newAlpha = ScaleByCurve(
                    panelAlpha.Evaluate(progress),
                    minPanelAlpha, panelAlphaRange);
                SetPanelAlpha(newAlpha);

                float newTextSize = ScaleByCurve(
                    textSize.Evaluate(progress),
                    minTextSize, textSizeRange);
                countdownText.fontSize = (int)newTextSize;

                elapsedTime += Time.deltaTime;
                progress = elapsedTime/countDuration;
                yield return null;
            }
            
            ++index;
        }
        HideCountdown();
        GameModel.instance.nc.NotifyMessage(Message.CountdownFinished, this);
    }

    void HideCountdown() {
        countdownText.text = "";
        SetPanelAlpha(0.0f);
    }

    float ScaleByCurve(float curveValue, float min, float range) {
        return min + curveValue * range;
    }

    void SetPanelAlpha(float newAlpha) {
        Color newColor = panelBackground.color;
        newColor.a = newAlpha;
        panelBackground.color = newColor;
    }
    
}

