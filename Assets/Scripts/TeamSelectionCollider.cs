using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UtilityExtensions;

public class TeamSelectionCollider : MonoBehaviour {

    public int teamNumber;
    int maxOnTeam = 2;
    public TeamManager team {get; set;}
    public bool mustDashToSwitch = true;
    Text countText;
    // Use this for initialization
    void Start () {
        countText = GetComponentInChildren<Text>();
        if (teamNumber < GameModel.instance.teams.Count) {
            team = GameModel.instance.teams[teamNumber];
        }
    }

    void OnCollisionStay2D(Collision2D collision) {
        var player = collision.gameObject.GetComponent<Player>();
        if (player != null && team != null && player.team != team) {
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
            if (countText != null) {
                countText.text = string.Format("{0}/{1}", team.teamMembers.Count, 2);
            }
            var renderer = GetComponent<SpriteRenderer>();
            if (team.teamMembers.Count >= maxOnTeam) {
                this.TimeDelayCall(() => {
                        AudioManager.instance.GoalSwitch.Play();
                        renderer.color = 0.85f * team.teamColor.color;
                    }, 0.3f);
            } else {
                renderer.color = team.teamColor;
            }
            lastCount = team.teamMembers.Count;
        }
    }
}
