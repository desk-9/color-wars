using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamManager {
    public int teamNumber { get; set; }
    public NamedColor teamColor { get; set; }

    public int score {get; private set;}
    List<Player> teamMembers = new List<Player>();

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
        GameModel.instance.Scored(this);
    }

    public void AddTeamMember(Player newMember) {
        teamMembers.Add(newMember);
    }

    public void FlashTeamColor() {
        foreach (var player in teamMembers) {
            player.FlashTeamColor();
        }
    }

}
