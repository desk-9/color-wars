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
    public int scoreMax = 7;
    public SceneStateController scene_controller {get; set;}
    public GameEndController end_controller {get; set;}
    
    IntCallback NextTeamAssignmentIndex;

    void Awake() {
        if (instance == null) {
            instance = this;
            Initialization();
        }
        else {
            Destroy(gameObject);
        }
    }

    void Initialization() {
        InitializeTeams();
        scene_controller = GetComponent<SceneStateController>();
        end_controller = GetComponent<GameEndController>();
    }

    public TeamManager GetTeamAssignment(Player caller) {
        var assignedTeam = teams[NextTeamAssignmentIndex()];
        assignedTeam.AddTeamMember(caller);
        return assignedTeam;
    }

    void InitializeTeams() {
        teams = new TeamManager[teamColors.Length];

        for (int i = 0; i < teamColors.Length; ++i) {
            // Add 1 so we get Team 1 and Team 2
            teams[i] = new TeamManager(i + 1, teamColors[i]);
        }
        NextTeamAssignmentIndex = Utility.ModCycle(0, teams.Length);
    }
    
    public void Scored(TeamManager team) {
        // One team just scored
        //
        // TODO: handle things like resetting the ball and players here, maybe
        // show UI elements
        if (team.score >= scoreMax) {
            end_controller.GameOver(team);
        }
    }
}
