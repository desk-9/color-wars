using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class ThermometerFill : MonoBehaviour
{

    public float minimum = 0.14f;
    public float maximum = 0.93f;
    private Image fillImage;

    private void Start()
    {
        fillImage = this.FindComponent<Image>("Fill");
        UpdateScore();
    }

    private int ScoreTemperature()
    {
        return 0; // GameModel.instance.fireTeam.score - GameModel.instance.iceTeam.score;
    }

    public void UpdateScore()
    {
        float scorePercent = ScoreTemperature() / ((float)GameModel.instance.requiredWinMargin);
        float startPoint = (minimum + maximum) / 2;
        float totalFillAmount = maximum - minimum;
        float endAmout = startPoint + scorePercent * totalFillAmount / 2;
        fillImage.fillAmount = endAmout;
    }
}
