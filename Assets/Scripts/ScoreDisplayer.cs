using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class ScoreDisplayer : MonoBehaviour {

    List<Text> teams;
    Text matchTimeText;
    void Start() {
        teams = new List<Text>() {
            transform.FindComponent<Text>("Team1Text"),
            transform.FindComponent<Text>("Team2Text")
        };
        matchTimeText = transform.FindComponent<Text>("MatchTimeText");
        StartCoroutine(InitScores());
    }

    IEnumerator InitScores() {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        UpdateScores();
    }

    public void StartMatchLengthUpdate(float matchLength) {
        StartCoroutine(UpdateMatchTime(matchLength));
    }

    IEnumerator UpdateMatchTime(float matchLength) {
        yield return new WaitForFixedUpdate();
        float startTime = Time.time;
        float endTime = startTime + matchLength;
        var start = DateTime.Now;
        var end = DateTime.Now.AddSeconds(matchLength);
        while (Time.time < endTime) {
            var now = start.AddSeconds(Time.time - startTime);
            var difference = end - now;
            var time_string = difference.ToString(@"mm\:ss");
            if (!PlayerTutorial.runTutorial) {
                matchTimeText.text = string.Format("Time: {0}", time_string);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    public void UpdateScores() {
        for (int i = 0; i < teams.Count && i < GameModel.instance.teams.Length; i++) {
            var text = teams[i];
            var team = GameModel.instance.teams[i];
            text.text = string.Format("{0} Team: {1}", team.teamColor.name, team.score);
            text.color = team.teamColor;
        }
    }
}
