using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JustInTimeTutorial : MonoBehaviour {
    Text display;
    void Start () {
        display = GetComponentInChildren<Text>();
        GameModel.instance.nc.CallOnMessage(
            Message.BallIsUnpossessed, Unpossessed);
        GameModel.instance.nc.CallOnMessageWithSender(
            Message.BallSetNeutral, PassToTeammate);
        GameModel.instance.nc.CallOnMessageWithSender(
            Message.BallCharged, ShootAtGoal);
        // On possession loss: no text
        // On possession with neutral: pass to teammate
        // On possession with charged: shoot at goal
    }

    void Unpossessed() {
        display.text = "";
    }

    void PassToTeammate(object sender) {
        var player = sender as Player;
        if (player != null && player.team != null && player.team.score == 0) {
            display.text = "Pass to your teammate";
        } else {
            display.text = "";
        }
    }

    void ShootAtGoal(object sender) {
        var player = sender as Player;
        if (player != null && player.team != null && player.team.score == 0) {
            display.text = "Shoot at the goal!";
        } else {
            display.text = "";
        }
    }
}
