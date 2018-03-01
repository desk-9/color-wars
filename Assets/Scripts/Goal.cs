using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Goal : MonoBehaviour {

	TeamManager current_team;
	// Use this for initialization
	void Start () {
		Debug.Log("start");
	}
	
	// Update is called once per frame
	void Update () {
		current_team = GameModel.instance.teams?[0];
	}

	// TODO: Change this to take some sort of ball component
	void ScoreGoal(GameObject thing) {
		current_team.IncrementScore();
	}
	
	void OnTriggerEnter2D(Collider2D collider) {
		// TODO: Change this to check for ball
		if (collider.GetComponent<PlayerMovement>()) {
			ScoreGoal(collider.gameObject);
		}
	}
	
}
