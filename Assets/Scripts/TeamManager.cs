using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamManager {
    public int teamNumber { get; set; }
    public Color teamColor { get; set; }

	int score = 0;
    List<Player> teamMembers = new List<Player>();

    public TeamManager(int teamNumber, Color teamColor)
    {
        this.teamNumber = teamNumber;
        this.teamColor = teamColor;
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

    public void AddTeamMember(Player newMember)
    {
        teamMembers.Add(newMember);
    }
    
}
