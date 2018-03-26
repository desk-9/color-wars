using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class ThermometerFill : MonoBehaviour {

    public float minimum = 0.14f;
    public float maximum = 0.93f;
    Image fillImage;
    void Start () {
        fillImage = this.FindComponent<Image>("Fill");
        UpdateScore();
    }

    int ScoreTemperature() {
        return GameModel.instance.fireTeam.score - GameModel.instance.iceTeam.score;
    }

    public void UpdateScore() {
        var scorePercent = ScoreTemperature() / ((float) GameModel.instance.requiredWinMargin);
        var startPoint = (minimum + maximum) / 2;
        var totalFillAmount = maximum - minimum;
        var endAmout = startPoint + scorePercent * totalFillAmount / 2;
        fillImage.fillAmount = endAmout;
    }
}
