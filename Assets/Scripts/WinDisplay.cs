using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class WinDisplay : MonoBehaviour {
    public int SecondsBeforeReset = 10;
    Text winnerText;
    Text restartTime;

    void Awake () {
        winnerText = transform.FindComponent<Text>("WinnerText");
        restartTime = transform.FindComponent<Text>("RestartText");
    }

    public void GameOverFunction() {
        Debug.Log("GameOverFunction!");
        var winner = GameModel.instance.winner;
        if (winner == null) {
            winnerText.text = "Tie!";
            winnerText.color = Color.black;
            return;
        }
        winnerText.text = string.Format("{0} Team won with {1} points",
                                        winner.teamColor.name, winner.score);
        winnerText.color = winner.teamColor;
        StartCoroutine(ResetCountdown());
    }

    IEnumerator ResetCountdown() {
        for (int i = SecondsBeforeReset; i > 0; --i) {
            restartTime.text = "Resetting in " + i.ToString() + "...";
            yield return new WaitForSeconds(1);
        }
        SceneStateController.instance.Load(Scene.Court);
    }

}
