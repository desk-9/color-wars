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

    GameObject tutorial;
    GameObject tutorialCanvas;
    Text tutorialText;
    Image tutorialImage;
    Text tutorialReadyText;
    string currentTutorialStage;

    List<TutorialStageInfo> tutorialInfo = new List<TutorialStageInfo>() {
        {"TeamsCorrect", "Dash at color to select team (teams of 2)", "AButton", TutorialRequirement.Called},
        {"PassSwitch", "Pass to your teammate to power up the ball", null},
        {"TimeoutShort", "Score into the goal when the ball is powered!", null, TutorialRequirement.ShortTimeout},
        {"Steal", "Dash at the ball when an enemy player holds to steal", null},
        {"TimeoutShort", "Stealing from a player stuns them", null, TutorialRequirement.ShortTimeout},
        {"Done", "Finished! Press A to play", "AButton"},
    };

    Dictionary<string, Callback> stageStarts = new Dictionary<string, Callback>();

    void Start() {

        stageStarts = new Dictionary<string, Callback>() {
            {"Steal", SetUpStealDummies},
            {"PassSwitch", SetUpPassSwitch},
            {"Done", SetUpDone}
        };

        if (runTutorial) {
            GameModel.playerTeamsAlreadySelected = false;
            GameModel.cheatForcePlayerAssignment = false;
            TeamManager.playerSpritesAlreadySet = false;
            tutorial = GameObject.Find("Tutorial");
            tutorialCanvas = tutorial.transform.Find("TutorialCanvas").gameObject;
            tutorialCanvas.SetActive(true);
            tutorialText = tutorialCanvas.FindComponent<Text>("TutorialText");
            tutorialImage = tutorialCanvas.FindComponent<Image>("TutorialImage");
            tutorialReadyText = GameObject.FindWithTag("TutorialReadyText").GetComponent<Text>();
            StartCoroutine(Tutorial());
        }
    }

    void SetTutorialElementActive(string name, bool activation) {
        tutorial.FindChild(name).SetActive(activation);
    }

    void SetUpStealDummies() {
        GameModel.instance.FlashScreen();
        ResetPlayerStates();
        MovePlayersToSpawnLocations();
        SetTutorialElementActive("TeamSelection", false);
        SetTutorialElementActive("SeperatedTeams", true);
        SetTutorialElementActive("SeperatedPlayers", true);
        SetTutorialElementActive("StealDummies", true);
        SetTutorialElementActive("PassBalls", false);
    }

    void SetUpPassSwitch() {
        GameModel.instance.FlashScreen();
        ResetPlayerStates();
        MovePlayersToSpawnLocations();
        SetTutorialElementActive("TeamSelection", false);
        SetTutorialElementActive("SeperatedTeams", true);
        SetTutorialElementActive("SeperatedPlayers", false);
        SetTutorialElementActive("StealDummies", false);
        SetTutorialElementActive("PassBalls", true);
    }

    void SetUpDone() {
        ResetPlayerStates();
        GameModel.instance.FlashScreen();
        SetTutorialElementActive("TeamSelection", false);
        SetTutorialElementActive("PassBalls", false);
        SetTutorialElementActive("SeperatedTeams", false);
        SetTutorialElementActive("SeperatedPlayers", false);
        SetTutorialElementActive("StealDummies", false);
    }

    void ResetPlayerStates() {
        foreach (var realPlayer in GameModel.instance.GetPlayersWithTeams()) {
            var stateManager = realPlayer.GetComponent<PlayerStateManager>();
            stateManager.CurrentStateHasFinished();
        }
    }

    void MovePlayersToSpawnLocations() {
        var spawnPoints = (new List<String>() {"PinkStartPoint", "BlueStartPoint"}).Select(
            s => GameObject.FindGameObjectWithTag(s)).ToList();
        Debug.Log(spawnPoints.Count);
        for (int i = 0; i < spawnPoints.Count; i++) {
            var spawnPoint = spawnPoints[i];
            foreach (var player in GameModel.instance.teams[i].teamMembers) {
                var angle = 180 * player.playerNumber;
                var displacement = new Vector2(0, 9);
                var finalDisplacement = Quaternion.AngleAxis(angle, Vector3.forward) * displacement;
                player.transform.position =
                    spawnPoint.transform.position + finalDisplacement;
            }
        }
    }

    List<Player> GetPlayerComponents() {
        var result = new List<Player>();
        foreach (var team in GameModel.instance.teams) {
            result.AddRange(team.teamMembers);
        }
        return result;
    }

    List<GameObject> GetPlayers() {
        return GetPlayerComponents().Select(p => p.gameObject).ToList();
    }

    Dictionary<object, bool> checkin = new Dictionary<object, bool>();
    void CheckinPlayer(object player) {
        checkin[player] = true;
        tutorialReadyText.text = string.Format(
            "{0}/{1}", GetPlayers().Count(p => checkin.Keys.Contains(p) && checkin[p]),
            GetPlayers().Count());
    }

    void ResetCheckin() {
        foreach (var player in GetPlayers()) {
            checkin[player] = false;
        }
    }

    void ResetStage() {
        ResetCheckin();
        timeoutShort = false;
        timeoutReallyShort = false;
        eventCalled = false;
    }

    bool AllCheckedIn() {
        bool all = true;
        foreach (var player in GetPlayers()) {
            if (player.GetComponent<PlayerMovement>().GetInputDevice() == null) {
                continue;
            }
            if (checkin.ContainsKey(player)) {
                // Debug.LogFormat("{0}: {1}", player, checkin[player]);
                if (!checkin[player]) {
                    all = false;
                }
            } else {
                all = false;
            }
        }
        return all;
    }

    bool AnyCheckedIn() {
        return checkin.Values.Any(i => i);
    }

    bool timeoutShort = false;
    bool timeoutReallyShort = false;

    bool eventCalled = false;

    bool StageDone(TutorialRequirement requirement) {
        if (requirement == TutorialRequirement.AllPlayers) {
            return AllCheckedIn();
        } else if (requirement == TutorialRequirement.AnyPlayer) {
            return AnyCheckedIn();
        } else if (requirement == TutorialRequirement.ShortTimeout) {
            return timeoutShort;
        } else if (requirement == TutorialRequirement.ReallyShortTimeout) {
            return timeoutReallyShort;
        } else if (requirement == TutorialRequirement.Called) {
            return eventCalled;
        } else {
            return false;
        }
    }

    void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(this);
        }
    }

    void StartShortTimeout() {
        this.RealtimeDelayCall(() => timeoutShort = true, 4f);
    }

    void StartReallyShortTimeout() {
        this.RealtimeDelayCall(() => timeoutReallyShort = true, 2f);
    }

    void SetTutorialText(string text) {
        tutorialText.text = text;
    }

    void SetTutorialImage(string imagePath) {
        Debug.Log("setting tutorial image");
        if (imagePath != null) {
            var image = Resources.Load<Sprite>(string.Format("TutorialImages/{0}", imagePath));
            tutorialImage.sprite = image;
            tutorialImage.color = Color.white;
        } else {
            tutorialImage.color = Color.clear;
        }
    }

    void SetInfoToIndex(int index) {
        var info = tutorialInfo[index];
        SetDisplayToInfo(info);
    }

    void SetDisplayToInfo(TutorialStageInfo info) {
        SetTutorialText(info.prompt);
        SetTutorialImage(info.imageResource);
    }

    IEnumerator Tutorial() {
        yield return null;
        foreach (var tutorialStage in tutorialInfo) {
            ResetStage();
            if (tutorialStage.eventString == "resetgoal") {
                GameModel.instance.goal?.ResetNeutral();
                continue;
            }
            if (tutorialStage.requirement == TutorialRequirement.AllPlayers) {
                tutorialReadyText.text = string.Format("0/{0}", GetPlayers().Count());
            } else {
                tutorialReadyText.text = "";
            }
            StartShortTimeout();
            StartReallyShortTimeout();
            SetDisplayToInfo(tutorialStage);
            if (stageStarts.ContainsKey(tutorialStage.eventString)) {
                stageStarts[tutorialStage.eventString]();
            }
            var eventString = tutorialStage.eventString + "Tutorial";
            bool stageOver = false;
            currentTutorialStage = eventString;
            GameModel.instance.nc.CallOnStringEventWithSender(
                currentTutorialStage,
                (object player) => {
                    if (eventString == currentTutorialStage) {
                        CheckinPlayer(player);
                    }
                });
            GameModel.instance.nc.CallOnStringEvent(
                currentTutorialStage,
                () => {
                    if (eventString == currentTutorialStage) {
                        eventCalled = true;
                    }
                });
            while (!StageDone(tutorialStage.requirement)) {
                yield return null;
            }
        }
        this.TimeDelayCall(TutorialFinished, 0.3f);
    }

    void TutorialFinished() {
        runTutorial = false;
        this.TimeDelayCall(() => GameModel.instance.FlashScreen(0.5f), 0.1f);
        this.TimeDelayCall(() => SceneStateController.instance.Load(Scene.Court), 0.4f);
    }

    void TeamSelectionFinished() {
        GameModel.playerTeamsAlreadySelected = true;
        GameModel.playerTeamAssignments = new Dictionary<int, int>();
        foreach (var player in GameModel.instance.GetPlayersWithTeams()) {
            var teamIndex = Array.FindIndex(
                GameModel.instance.teams, team => team == player.team);
            GameModel.playerTeamAssignments[player.playerNumber] = teamIndex;
        }
        TeamManager.playerSpritesAlreadySet = true;
    }

    void Update() {
        if (currentTutorialStage == "TeamsCorrectTutorial") {
            if (GameModel.instance.teams.All(team => team.teamMembers.Count == 2)) {
                Utility.TutEvent("TeamsCorrect", this);
                TeamSelectionFinished();
            }
        }
    }
}
