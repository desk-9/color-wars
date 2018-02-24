using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamManager : MonoBehaviour {

    public int teamNumber;

    int score = 0;

	void Start () {
        ScoreDisplayer.instance.RegisterTeam(teamNumber);
	}

    public void ResetScore() {
        score = 0;
        ScoreDisplayer.instance.SetScore(teamNumber, score);
    }
    
    public void IncrementScore() {
        ++score;
        ScoreDisplayer.instance.SetScore(teamNumber, score);
    }
    
}
