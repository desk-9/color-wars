using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class TeamSelectionCollider : MonoBehaviour {

    public int teamNumber;
    int maxOnTeam = 2;
    public TeamManager team {get; set;}
    public bool mustDashToSwitch = true;
    // Use this for initialization
    void Start () {
        if (teamNumber < GameModel.instance.teams.Length) {
            team = GameModel.instance.teams[teamNumber];
            GetComponent<SpriteRenderer>().color = team.teamColor + 0.1f * Color.white;
        }
    }

    void OnCollisionEnter2D(Collision2D collision) {
        var player = collision.gameObject.GetComponent<Player>();
        if (player != null && team != null) {
            var stateManager = player.GetComponent<PlayerStateManager>();
            if (stateManager != null) {
                if (mustDashToSwitch && !stateManager.IsInState(State.Dash)) {
                    // Only switch if dashing
                    return;
                }
            }
            if (player.team != team && team.teamMembers.Count < maxOnTeam) {
                player.SetTeam(team);
                AudioManager.instance.Beep.Play();
            }
        }
    }
    int lastCount = 0;
    void FixedUpdate() {
        if (team != null && team.teamMembers.Count != lastCount) {
            var renderer = GetComponent<SpriteRenderer>();
            if (team.teamMembers.Count >= maxOnTeam) {
                this.TimeDelayCall(() => {
                        AudioManager.instance.GoalSwitch.Play();
                        renderer.color = team.teamColor + 0.4f * Color.white;
                    }, 0.3f);
            } else {
                renderer.color = team.teamColor + 0.1f * Color.white;
            }
            lastCount = team.teamMembers.Count;
        }
    }
}
