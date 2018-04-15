using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class JustInTimeTutorial : MonoBehaviour {
    public static bool alreadySeen = false;
    int scoreThreshold = 0;
    Text display;
    void Start () {
        display = GetComponentInChildren<Text>();
        GameModel.instance.nc.CallOnMessage(
            Message.BallIsUnpossessed, Unpossessed);
        GameModel.instance.nc.CallOnMessageWithSender(
            Message.BallPossessedWhileNeutral, PassToTeammate);
        GameModel.instance.nc.CallOnMessageWithSender(
            Message.BallPossessedWhileCharged, ShootAtGoal);
        GameModel.instance.nc.CallOnMessage(
            Message.GoalScored,
            () => {
                if (!alreadySeen && GameModel.instance.teams.All(
                        team => team.score > scoreThreshold)) {
                    alreadySeen = true;
                }
            });

        GameModel.instance.nc.CallOnMessage(
            Message.PlayerReleasedBack,
            () => {
                Debug.LogError("Pressed back");
                scoreThreshold = GameModel.instance.teams.Max(team => team.score);
                alreadySeen = false;
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
        if (!alreadySeen && player != null && player.team != null
            && player.team.score <= scoreThreshold) {
            display.text = "Pass to your teammate";
        } else {
            display.text = "";
        }
    }

    void ShootAtGoal(object sender) {
        var player = sender as Player;
        if (!alreadySeen && player != null && player.team != null
            && player.team.score <= scoreThreshold) {
            display.text = "Shoot at the goal";
        } else {
            display.text = "";
        }
    }
}
