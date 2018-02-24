using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreDisplayer : MonoBehaviour {

    public Text scoreDisplay;

    void Start() {
        StartCoroutine(InitScores());
    }

    IEnumerator InitScores() {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        UpdateScores();
    }

    public void UpdateScores() {
        string scoreText = "";
        foreach (var team in GameModel.instance.teams) {
            scoreText += "Team " + team.teamNumber.ToString() + ": " + team.GetScore().ToString() + "\n";
        }
        scoreDisplay.text = scoreText;
    }
    

}
