using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class EndGameText : MonoBehaviour
{
    private Text endText;
    public AnimationCurve textSize;
    public float minTextSize = 75;
    public float maxTextSize = 200;
    public float textLerpDuration;
    public string endTextContent = "Game!";

    private void Start()
    {
        endText = transform.FindComponent<Text>("EndText");
        endText.text = "";
        GameManager.Instance.OnGameOver += () => GameOverFunction();
    }

    public void GameOverFunction()
    {
        endText.color = GameManager.Instance.Winner.TeamColor.color;
        endText.text = endTextContent;

        // Start "Game!" text lerp
        StartCoroutine(
            TransitionUtility.LerpFloat(
                (float value) =>
                {
                    float scaledProgress = textSize.Evaluate(value);
                    endText.fontSize = (int)Mathf.Lerp(
                        minTextSize, maxTextSize, scaledProgress);
                },
                0.0f, 1.0f,
                textLerpDuration));
    }
}
