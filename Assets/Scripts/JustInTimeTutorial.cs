using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class JustInTimeTutorial : MonoBehaviour {
    public static JustInTimeTutorial instance;
    public static bool alreadySeen = false;
    int scoreThreshold = 0;
    GameObject canvasPrefab;

    void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(this);
        }
    }

    void Start () {
        canvasPrefab = Resources.Load<GameObject>("ToolTipCanvas");
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

        GameModel.instance.nc.CallOnStateEnd(State.Posession, Unpossessed);

        GameModel.instance.nc.CallOnMessage(
            Message.PlayerReleasedBack,
            () => {
                scoreThreshold = GameModel.instance.teams.Max(team => team.score);
                alreadySeen = false;
            });
        // On possession loss: no text
        // On possession with neutral: pass to teammate
        // On possession with charged: shoot at goal
    }

    void Unpossessed(Player player) {
        var tooltipCanvas = player.GetComponentInChildren<ToolTipPlacement>();
        if (tooltipCanvas != null) {
            tooltipCanvas.SetText("");
        }
    }

    ToolTipPlacement CheckMakeCanvas(Player player) {
        var tooltipCanvas = player?.GetComponentInChildren<ToolTipPlacement>();
        if (tooltipCanvas == null && player != null) {
            tooltipCanvas = Instantiate(canvasPrefab, player.transform).GetComponent<ToolTipPlacement>();
        }
        return tooltipCanvas;
    }

    void PassToTeammate(object sender) {
        var player = sender as Player;
        var tooltipCanvas = CheckMakeCanvas(player);
        if (!alreadySeen && player != null && player.team != null
            && player.team.score <= scoreThreshold) {
            tooltipCanvas?.SetText("<AButton> Pass to your teammate");
        } else {
            tooltipCanvas?.SetText("");
        }
    }

    void ShootAtGoal(object sender) {
        var player = sender as Player;
        var tooltipCanvas = CheckMakeCanvas(player);
        if (!alreadySeen && player != null && player.team != null
            && player.team.score <= scoreThreshold) {
            tooltipCanvas?.SetText("<AButton> Shoot at the goal");
        } else {
            tooltipCanvas?.SetText("");
        }
    }
}
