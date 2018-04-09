using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UtilityExtensions;
using IC = InControl;

public enum TutorialRequirement {
    AllPlayers,
    AnyPlayer,
    ShortTimeout,
    ReallyShortTimeout,
    Called
};

public struct TutorialStageInfo {
    public string eventString;
    public string prompt;
    public string imageResource;
    public TutorialRequirement requirement;

    public TutorialStageInfo(string eventString, string prompt,
                             string imageResource,
                             TutorialRequirement requirement = TutorialRequirement.AllPlayers) {
        this.eventString = eventString;
        this.prompt = prompt;
        this.imageResource = imageResource;
        this.requirement = requirement;
    }
}

public enum TutorialType {
    TeamSelection,
    Sandbox,
    None
}

public class PlayerTutorial : MonoBehaviour {
    public static PlayerTutorial instance;
    public static bool runTutorial = false;
    public TutorialType tutorialType = TutorialType.None;


    int currentStageNumber = -1;
    Dictionary<GameObject, bool> checkin = new Dictionary<GameObject, bool>();
    Text readyUpText;
    Text readyUpCount;
    bool skipReadyUpCheat = false;

    bool inTeamSelection = false;

    void Start() {
        readyUpText = GameObject.Find("ReadyUpText")?.GetComponent<Text>();
        readyUpCount = GameObject.Find("ReadyUpCount")?.GetComponent<Text>();
        inTeamSelection = (tutorialType == TutorialType.TeamSelection
                           && GameObject.Find("TeamSelection") != null);
        if (inTeamSelection) {
            StartCoroutine(TeamSelection());
        } else if (tutorialType == TutorialType.Sandbox) {
            StartCoroutine(Sandbox());
        }
    }

    void StartListeningForPlayers() {
        GameModel.instance.nc.CallOnMessageWithSender(
            Message.PlayerReleasedX, CheckinPlayer);
        GameModel.instance.nc.CallOnMessage(
            Message.PlayerPressedLeftBumper, () => skipReadyUpCheat = true);
    }

    List<GameObject> GetPlayers() {
        return (from player in GameModel.instance.GetHumanPlayers()
                select player.gameObject).ToList();
    }

     void CheckinPlayer(object potentialPlayer) {
        var player = potentialPlayer as GameObject;
        if (player != null) {
            checkin[player] = true;
        }
        readyUpCount.text = string.Format("{0}/{1}", NumberCheckedIn(), GetPlayers().Count);
    }

    void ResetCheckin() {
        skipReadyUpCheat = false;
        foreach (var player in GetPlayers()) {
            checkin[player.gameObject] = false;
        }
        readyUpCount.text = string.Format("{0}/{1}", NumberCheckedIn(), GetPlayers().Count);
    }

    int NumberCheckedIn() {
        return GetPlayers().Count(player => checkin.GetDefault(player, false));
    }

    bool AllCheckedIn() {
        var allPlayers = (from player in GetPlayers() select checkin[player]).All(x => x);
        return allPlayers || skipReadyUpCheat;
    }

    void TeamSelectionFinished() {
        inTeamSelection = false;
        StartCoroutine(EndTutorial());
    }

    IEnumerator EndTutorial() {
        readyUpText.text = "Press X to start the tutorial";
        ResetCheckin();
        StartListeningForPlayers();
        yield return null;
        while (!AllCheckedIn()) {
            yield return null;
        }
        yield return new WaitForSeconds(0.5f);
        GameModel.playerTeamsAlreadySelected = true;
        GameModel.playerTeamAssignments = new Dictionary<int, int>();
        foreach (var player in GameModel.instance.GetPlayersWithTeams()) {
            var teamIndex = GameModel.instance.teams.IndexOf(player.team);
            GameModel.playerTeamAssignments[player.playerNumber] = teamIndex;
        }
        TeamManager.playerSpritesAlreadySet = true;
        SceneStateController.instance.Load(Scene.Tutorial);

    }

    IEnumerator TeamSelection() {
        yield return new WaitForFixedUpdate();
        while (true) {
            if (inTeamSelection && GameModel.instance.teams.All(team => team.teamMembers.Count == 2)) {
                TeamSelectionFinished();
                yield break;
            }
            yield return null;
        }
    }

    List<Tuple<string, Message>> stages = new List<Tuple<string, Message>>() {
        {"Try laying a wall with B", Message.PlayerReleasedWall},
        {"Press X to start the game!", Message.PlayerPressedX},
    };

    void StageCheckinListen(int stageNumber, Message playerEvent) {
        GameModel.instance.nc.CallOnMessageIf(
            playerEvent, CheckinPlayer, _ => currentStageNumber == stageNumber);
        GameModel.instance.nc.CallOnMessage(
            Message.PlayerPressedLeftBumper, () => skipReadyUpCheat = true);
    }

    IEnumerator Sandbox() {
        yield return null;
        for (int i = 0; i < stages.Count; i++) {
            currentStageNumber = i;
            var stageText = stages[i].Item1;
            var stageEvent = stages[i].Item2;
            readyUpText.text = stageText;
            ResetCheckin();
            StageCheckinListen(i, stageEvent);
            yield return null;
            while (!AllCheckedIn()) {
                yield return null;
            }
        }
        PlayerTutorial.runTutorial = false;
        SceneStateController.instance.Load(Scene.Court);
    }
}
