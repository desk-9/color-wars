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

public class PlayerTutorial : MonoBehaviour {
    public static PlayerTutorial instance;
    public static bool runTutorial = false;

    Dictionary<GameObject, bool> checkin = new Dictionary<GameObject, bool>();
    Text readyUpText;
    Text readyUpCount;
    bool skipReadyUpCheat = false;

    bool inTeamSelection = false;

    void Start() {
        readyUpText = GameObject.Find("ReadyUpText")?.GetComponent<Text>();
        readyUpCount = GameObject.Find("ReadyUpCount")?.GetComponent<Text>();
        inTeamSelection = (GameObject.Find("TeamSelection") != null);
    }

    void StartListeningForPlayers() {
        GameModel.instance.nc.CallOnMessageWithSender(
            Message.PlayerReleasedA, CheckinPlayer);
        GameModel.instance.nc.CallOnMessage(
            Message.PlayerPressedLeftBumper, () => skipReadyUpCheat = true);
    }

    List<GameObject> GetPlayers() {
        return (from player in GameModel.instance.GetAllPlayers()
                select player.gameObject).ToList();
    }

     void CheckinPlayer(object potentialPlayer) {
        var player = potentialPlayer as GameObject;
        if (player != null) {
            checkin[player] = true;
        }
        Utility.Print("Checking in player", player, checkin[player], NumberCheckedIn());
        readyUpCount.text = string.Format("{0}/{1}", NumberCheckedIn(), GetPlayers().Count);
    }

    void ResetCheckin() {
        skipReadyUpCheat = false;
        foreach (var player in GetPlayers()) {
            checkin[player.gameObject] = false;
        }
        Utility.Print("Reseting count");
        readyUpCount.text = string.Format("{0}/{1}", NumberCheckedIn(), GetPlayers().Count);
    }

    int NumberCheckedIn() {
        return GetPlayers().Count(player => checkin[player]);
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
        readyUpText.text = "Press A to start the tutorial";
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

    void Update() {
        if (inTeamSelection) {
            if (GameModel.instance.teams.All(team => team.teamMembers.Count == 2)) {
                TeamSelectionFinished();
            }
        }
    }
}
