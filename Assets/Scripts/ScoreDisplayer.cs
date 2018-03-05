using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class ScoreDisplayer : MonoBehaviour {

    List<Text> teams;
    void Start() {
        teams = new List<Text>() {
            transform.FindComponent<Text>("Team1Text"),
            transform.FindComponent<Text>("Team2Text")
        };
        StartCoroutine(InitScores());
    }

    IEnumerator InitScores() {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        UpdateScores();
    }

    public void UpdateScores() {
        for (int i = 0; i < teams.Count && i < GameModel.instance.teams.Length; i++) {
            var text = teams[i];
            var team = GameModel.instance.teams[i];
            text.text = string.Format("Team {0}: {1}", team.teamNumber, team.score);
            text.color = team.teamColor;
        }
    }
}
