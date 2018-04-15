using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class JustInTimeTutorial : MonoBehaviour {
    public static bool alreadySeen = false;
    Text display;
    void Start () {
        display = GetComponentInChildren<Text>();
        GameModel.instance.nc.CallOnMessage(
            Message.BallIsUnpossessed, Unpossessed);
        GameModel.instance.nc.CallOnMessageWithSender(
            Message.BallSetNeutral, PassToTeammate);
        GameModel.instance.nc.CallOnMessageWithSender(
            Message.BallCharged, ShootAtGoal);
        GameModel.instance.nc.CallOnMessage(
            Message.GoalScored,
            () => {
                if (!alreadySeen && GameModel.instance.teams.All(team => team.score >= 0)) {
                    alreadySeen = true;
                }
            });
        // On possession loss: no text
        // On possession with neutral: pass to teammate
        // On possession with charged: shoot at goal
    }

    void Unpossessed() {
        display.text = "";
    }

    void PassToTeammate(object sender) {
        var player = sender as Player;
        if (!alreadySeen && player != null && player.team != null && player.team.score == 0) {
            display.text = "Pass to your teammate";
        } else {
            display.text = "";
        }
    }

    void ShootAtGoal(object sender) {
        var player = sender as Player;
        if (!alreadySeen && player != null && player.team != null && player.team.score == 0) {
            display.text = "Shoot at the goal";
        } else {
            display.text = "";
        }
    }
}
