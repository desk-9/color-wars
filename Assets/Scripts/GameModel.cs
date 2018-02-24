using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameModel : MonoBehaviour {

    public static GameModel instance;
	public List<TeamManager> teams = new List<TeamManager>();
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
			return;
        }
		teams.Add(team);
    }

}
