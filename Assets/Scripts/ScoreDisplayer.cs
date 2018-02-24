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
        foreach (var score in GameModel.instance.scores) {
            scoreText += "Team " + score.Key.ToString() + ": " + score.Value.ToString() + "\n";
        }
        scoreDisplay.text = scoreText;
    }
    

}
