using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using IC = InControl;
using UtilityExtensions;


public class GameModel : MonoBehaviour {

    public static GameModel instance;
    public ScoreDisplayer scoreDisplayer;
    public NamedColor[] teamColors;
    public TeamManager[] teams { get; set; }
    public int scoreMax = 7;
    // public GameEndController end_controller {get; set;}
    public float matchLength = 5f;
    public NotificationCenter nc;
    public bool gameOver {get; private set;} = false;
    public TeamManager winner {get; private set;} = null;
    public GameObject meta;

    public Callback OnGameOver = delegate{};

    float matchLengthSeconds;
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
        meta = SceneStateController.instance.gameObject;
        if (meta == null) {
            Debug.LogWarning("Meta object is null!!!!");
        }
        matchLengthSeconds = 60 * matchLength;
        this.TimeDelayCall(EndGame, matchLengthSeconds);
        nc = new NotificationCenter();
    }

    void Start() {
        scoreDisplayer?.StartMatchLengthUpdate(matchLengthSeconds);
    }

    void EndGame() {
        Debug.Log("game over");
        winner = teams.Aggregate(
            (bestSoFar, next) => {
                if (bestSoFar == null || bestSoFar.score == next.score) {
                    return null;
                }
                return next.score > bestSoFar.score ? next : bestSoFar;
            });
        OnGameOver();
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

    }

    
}
