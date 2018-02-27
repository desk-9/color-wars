using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;


public class GameModel : MonoBehaviour {

    public static GameModel instance;
    public ScoreDisplayer scoreDisplayer;
    public int numTeams;
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
        var assignedTeam = teams[nextTeamAssignmentIndex++ % teams.Length];
        assignedTeam.AddTeamMember(caller);
        return assignedTeam;
    }

    void InitializeTeams()
    {
        teams = new TeamManager[numTeams];

        for (int i = 0; i < numTeams; ++i) {
            // Add 1 so we get Team 1 and Team 2
            teams[i] = new TeamManager(i + 1);
        }
    }
}
