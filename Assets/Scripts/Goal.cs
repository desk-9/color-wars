using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UtilityExtensions;

public class Goal : MonoBehaviour {

	public TeamManager current_team;
	IntCallback next_team_index;
	void Start () {
		current_team = GameModel.instance.teams?.First();
		next_team_index = Utility.ModCycle(0, GameModel.instance.teams.Length);
	}

	TeamManager NextTeam() {
		return GameModel.instance.teams[next_team_index()];
	}
	
	void ScoreGoal(Ball ball) {
		current_team?.IncrementScore();
		// TODO: Non-tweakable placeholder delay on ball reset until it's
		// decided what should happen respawn-wise on goal scoring
		this.TimeDelayCall(ball.ResetBall, 0.35f);
	}
	
	void OnTriggerEnter2D(Collider2D collider) {
		var ball = collider.gameObject.GetComponent<Ball>();
		if (ball != null) {
			ScoreGoal(ball);
		}
	}

	void Update() {
		// Test code for switching goal team until full timed team switching is implemented
		var left_bumper = InControl.InputControlType.LeftBumper;
		if (InControl.InputManager.ActiveDevice.GetControl(left_bumper).WasPressed) {
			Debug.Log("Pressed");
			current_team = NextTeam();
		}
	}
}
