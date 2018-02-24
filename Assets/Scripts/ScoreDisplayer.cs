using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreDisplayer : MonoBehaviour {

    public Text ScoreDisplay;
    public SortedDictionary<int, int> scores = new SortedDictionary<int, int>();

    public static ScoreDisplayer instance;
    void Awake() {
        if (instance == null) {
            instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    void Start() {
        StartCoroutine(InitScores());
    }

    public void RegisterTeam(int teamNumber) {
        if (scores.ContainsKey(teamNumber)) {
            Debug.LogWarning("Tried to register team " + teamNumber.ToString() + " twice!");
            return;
        }
        scores[teamNumber] = 0;
    }

    IEnumerator InitScores() {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        UpdateScores();
    }

    void UpdateScores() {
        string scoreText = "";
        foreach (var score in scores) {
            scoreText += "Team " + score.Key.ToString() + ": " + score.Value.ToString() + "\n";
        }
        ScoreDisplay.text = scoreText;
    }

    public void SetScore(int teamNumber, int newScore) {
        scores[teamNumber] = newScore;
        UpdateScores();
    }
    

}
