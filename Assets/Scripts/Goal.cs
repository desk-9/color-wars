using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UtilityExtensions;

public class Goal : MonoBehaviour {

	public TeamManager current_team;
    public float goal_switch_interval = 10;

	IntCallback next_team_index;
    new SpriteRenderer renderer;

    void Awake() {
        renderer = GetComponent<SpriteRenderer>();
    }
    
	void Start () {
		next_team_index = Utility.ModCycle(0, GameModel.instance.teams.Length);
        SwitchToNextTeam();
        StartCoroutine(TeamSwitching());
	}

    IEnumerator TeamSwitching() {
        while (true) {
            yield return new WaitForSeconds(goal_switch_interval);
            SwitchToNextTeam();
        }
    }

	TeamManager GetNextTeam() {
		return GameModel.instance.teams[next_team_index()];
	}

    void SwitchToNextTeam() {
        current_team = GetNextTeam();
        if (renderer != null) {
            renderer.color = current_team.teamColor;
        }
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
}
