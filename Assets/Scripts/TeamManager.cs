using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamManager : MonoBehaviour {

	public int teamNumber = 0;

	int score = 0;
	
	void Start () {
        GameModel.instance.RegisterTeam(this);
	}

	public void ResetScore() {
		score = 0;
		GameModel.instance.scoreDisplayer?.UpdateScores();
	}
	public void IncrementScore() {
		score += 1;
		GameModel.instance.scoreDisplayer?.UpdateScores();
	}
	public int GetScore() {
		return score;
	}
    
}
