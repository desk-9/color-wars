using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamManager {
    public int teamNumber { get; set; }
    public NamedColor teamColor { get; set; }

    public int score {get; private set;}
    public List<Player> teamMembers = new List<Player>();

    public TeamManager(int teamNumber, NamedColor teamColor) {
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

    public void AddTeamMember(Player newMember) {
        teamMembers.Add(newMember);
    }

    public void MakeInvisibleAfterGoal() {
        foreach (var teamMember in teamMembers) {
            teamMember.MakeInvisibleAfterGoal();
        }
    }

    public void ResetTeam() {
        foreach (var teamMember in teamMembers) {
            teamMember.ResetPlayerPosition();
        }
    }

    public void BeginMovement() {
        foreach (var teamMember in teamMembers) {
            teamMember.BeginPlayerMovement();
        }
    }
}
