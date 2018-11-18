using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class ScoreDisplayer : MonoBehaviour
{
    private List<Text> teams;
    private Text matchTimeText;

    private void Start()
    {
        teams = new List<Text>() {
            transform.FindComponent<Text>("Team1Text"),
            transform.FindComponent<Text>("Team2Text")
        };
        matchTimeText = transform.FindComponent<Text>("MatchTimeText");
        StartCoroutine(InitScores());
    }

    private IEnumerator InitScores()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        UpdateScores();
    }

    public void StartMatchLengthUpdate(float matchLength)
    {
        StartCoroutine(UpdateMatchTime(matchLength));
    }

    private IEnumerator UpdateMatchTime(float matchLength)
    {
        yield return new WaitForFixedUpdate();
        float startTime = Time.time;
        float endTime = startTime + matchLength;
        DateTime start = DateTime.Now;
        DateTime end = DateTime.Now.AddSeconds(matchLength);
        while (Time.time < endTime)
        {
            DateTime now = start.AddSeconds(Time.time - startTime);
            TimeSpan difference = end - now;
            string time_string = difference.ToString(@"mm\:ss");
            if (!PlayerTutorial.runTutorial)
            {
                matchTimeText.text = string.Format("Time: {0}", time_string);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    public void UpdateScores()
    {
        for (int i = 0; i < teams.Count && i < GameManager.instance.teams.Count; i++)
        {
            Text text = teams[i];
            TeamManager team = GameManager.instance.teams[i];
            text.text = string.Format("{0} Team: {1}", team.teamColor.name, team.score);
            text.color = team.teamColor;
        }
    }
}
