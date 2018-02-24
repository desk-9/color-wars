using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameModel : MonoBehaviour {

    public static GameModel instance;
    public SortedDictionary<int, int> scores = new SortedDictionary<int, int>();

	List<TeamManager> teams = new List<TeamManager>();
	public ScoreDisplayer scoreDisplayer;

    void Awake() {
        
        if (instance == null) {
            instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    public void RegisterTeam(TeamManager team) {
        if (teams.Contains(team)) {
            Debug.LogWarning("Trying to register team twice!");
        }
		teams.Add(team);
		
		int teamNumber = team.teamNumber;
        if (scores.ContainsKey(teamNumber)) {
            Debug.LogWarning("Trying to register team twice!");
        }
        scores[teamNumber] = 0;

    }


    public void ResetScore(TeamManager team) {
        scores[team.teamNumber] = 0;
        scoreDisplayer?.UpdateScores();
    }
    
    public void IncrementScore(TeamManager team) {
        ++scores[team.teamNumber];
        scoreDisplayer?.UpdateScores();
    }

}
