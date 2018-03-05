using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class WinDisplay : MonoBehaviour {
    Text winnerText;
    Text restartTime;
    void Awake () {
        winnerText = transform.FindComponent<Text>("WinnerText");
        restartTime = transform.FindComponent<Text>("RestartText");
    }

    public void SetWinner(TeamManager winner) {
        winnerText.text = string.Format("Team {0} won with {1} points",
                                        winner.teamNumber, winner.score);
        winnerText.color = winner.teamColor;
    }

    public void SetRestartTime(float time) {
        restartTime.text = string.Format("Restarting in {0:n0}...", time);
    }
}
