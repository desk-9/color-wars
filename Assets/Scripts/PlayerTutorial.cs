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
    public float tutorialStartTime = 7;
    public float gameStartTime = 5;

    Dictionary<GameObject, bool> checkin = new Dictionary<GameObject, bool>();
    Text readyUpText;
    Text readyUpCount;
    Text skipText;
    Text skipCount;
    bool skipReadyUpCheat = false;

    bool inTeamSelection = false;

    PlayerCheckin startTutorialCheckin;
    PlayerCheckin skipTutorialCheckin;

    void Start() {
        readyUpText = GameObject.Find("ReadyUpText")?.GetComponent<Text>();
        readyUpCount = GameObject.Find("ReadyUpCount")?.GetComponent<Text>();
        skipText = GameObject.Find("SkipText")?.GetComponent<Text>();
        skipCount = GameObject.Find("SkipCount")?.GetComponent<Text>();
        inTeamSelection = (tutorialType == TutorialType.TeamSelection
                           && GameObject.Find("TeamSelection") != null);

        GameModel.instance.nc.CallOnMessage(
            Message.PlayerPressedLeftBumper, () => skipReadyUpCheat = true);

        skipTutorialCheckin = PlayerCheckin.TextCountCheckin(
            () => GetPlayers(), Message.PlayerPressedY, skipCount,
            checkoutEvent: Message.PlayerReleasedY);

        if (inTeamSelection) {
            StartCoroutine(TeamSelection());
        } else if (tutorialType == TutorialType.Sandbox) {
            StartCoroutine(Sandbox());
        }
    }

    public static void SkipTutorial() {
        PlayerTutorial.runTutorial = false;
        SceneStateController.instance.Load(Scene.Court);
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
        GameModel.playerTeamsAlreadySelected = true;
        GameModel.playerTeamAssignments = new Dictionary<int, int>();
        foreach (var player in GameModel.instance.GetPlayersWithTeams()) {
            var teamIndex = GameModel.instance.teams.IndexOf(player.team);
            GameModel.playerTeamAssignments[player.playerNumber] = teamIndex;
        }
        TeamManager.playerSpritesAlreadySet = true;


        skipTutorialCheckin.ResetCheckin();
        skipTutorialCheckin.StartListening();
        skipText.text = "Hold (Y) to skip the tutorial";
        // Start the countdown.
        var start = Time.time;
        var diff = Time.time - start;
        // TODO: if slowmo becomes possible here might wanna use realtime instead
        while (diff < tutorialStartTime
               && !skipTutorialCheckin.AllCheckedIn()
               && !skipReadyUpCheat) {
            readyUpText.text = String.Format("Starting tutorial in {0:N0}",
                                             Mathf.Ceil(tutorialStartTime - diff));
            diff = Time.time - start;
            yield return null;
        }

        // Switch to Tutorial scene.
        yield return null;
        skipTutorialCheckin.StopListening();
        yield return new WaitForSeconds(0.5f);
        if (skipTutorialCheckin.AllCheckedIn()) {
            SkipTutorial();
        } else {
            SceneStateController.instance.Load(Scene.Tutorial);
        }

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

    IEnumerator Sandbox() {
        yield return null;

        // Press B to lay a wall!
        readyUpText.text = "Try laying a wall with B";

        ResetCheckin();
        GameModel.instance.nc.CallOnMessageWithSender(
            Message.PlayerReleasedWall, CheckinPlayer
        );
        yield return null;
        while (!AllCheckedIn()) {
            yield return null;
        }
        ResetCheckin();
        GameModel.instance.nc.UnsubscribeMessage(Message.PlayerReleasedWall, CheckinPlayer);
        readyUpCount.text = "";

        // Start the countdown.
        var start = Time.time;
        var diff = Time.time - start;
        // TODO: if slowmo becomes possible here might wanna use realtime instead
        while (diff < gameStartTime && !skipReadyUpCheat) {
            readyUpText.text = String.Format("Starting the game in {0:N0}", Mathf.Ceil(gameStartTime - diff));
            diff = Time.time - start;
            yield return null;
        }

        SkipTutorial();
    }
}
