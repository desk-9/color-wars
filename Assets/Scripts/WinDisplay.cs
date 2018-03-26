using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class WinDisplay : MonoBehaviour {
    public string mainMenuInstructions = "Press dpad-up to return to main menu";
    public int SecondsBeforeReset = 10;
    Text winnerText;
    Text restartTime;
    Text loserText;

    void Start () {
        winnerText = transform.FindComponent<Text>("WinnerText");
        restartTime = transform.FindComponent<Text>("RestartText");
    }

    public void GameOverFunction() {
        winnerText = transform.FindComponent<Text>("WinnerText");
        restartTime = transform.FindComponent<Text>("RestartText");
        loserText = transform.FindComponent<Text>("LoserText");
        Debug.Log("GameOverFunction!");
        var winner = GameModel.instance.winner;
        if (winner == null) {
            winnerText.text = "Tie!";
            winnerText.color = Color.black;
        } else {
            var otherTeam = new List<TeamManager>(GameModel.instance.teams).Find(team => team != winner);
            winnerText.text = string.Format("{0} Team won!", winner.teamColor.name);
            winnerText.color = winner.teamColor;
            loserText.text = string.Format("{0} Team lost...", otherTeam.teamColor.name);
            loserText.color = otherTeam.teamColor;
        }
        StartCoroutine(ResetCountdown());
    }

    IEnumerator ResetCountdown() {
        for (int i = SecondsBeforeReset; i > 0; --i) {
            restartTime.text = "resetting in " + i.ToString() + "...\n" + mainMenuInstructions;
            yield return new WaitForSeconds(1);
        }
        SceneStateController.instance.Load(Scene.Court);
    }

}
