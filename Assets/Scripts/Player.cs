using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour {

    TeamManager team;

	// Use this for initialization
	void Start () {
        team = GameModel.instance.GetTeamAssignment(this);
        Debug.LogFormat("Assigned player {0} to team {1}", name, team.teamNumber);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
