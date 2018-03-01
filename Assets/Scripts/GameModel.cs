using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using IC = InControl;


public class GameModel : MonoBehaviour {

    public static GameModel instance;
    public ScoreDisplayer scoreDisplayer;
    public Color[] teamColors;
    public TeamManager[] teams { get; set; }

    int nextTeamAssignmentIndex = 0;

    void Awake() {
        if (instance == null) {
            instance = this;
            InitializeTeams();
        }
        else {
            Destroy(gameObject);
        }
    }

    public TeamManager GetTeamAssignment(Player caller)
    {
        var assignedTeam = teams[nextTeamAssignmentIndex];
        nextTeamAssignmentIndex = (nextTeamAssignmentIndex + 1) % teams.Length;
        assignedTeam.AddTeamMember(caller);
        return assignedTeam;
    }

    void InitializeTeams()
    {
        teams = new TeamManager[teamColors.Length];

        for (int i = 0; i < teamColors.Length; ++i) {
            // Add 1 so we get Team 1 and Team 2
            teams[i] = new TeamManager(i + 1, teamColors[i]);
        }
    }
}
