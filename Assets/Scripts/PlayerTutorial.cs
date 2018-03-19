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

    GameObject tutorialCanvas;
    Text tutorialText;
    Image tutorialImage;
    Text tutorialReadyText;
    string currentTutorialStage;

    List<TutorialStageInfo> tutorialInfo = new List<TutorialStageInfo>() {
        {"Move", "Move with the joystick", "LeftThumbstick"},
        {"Dash", "Press A to quick dash", "AButton"},
        {"DashCharge", "Hold A for longer dash", "AButton"},
        {"BallPickup", "Run into the ball to pick it up", null, TutorialRequirement.AnyPlayer},
        {"Shoot", "Press A to shoot", "AButton", TutorialRequirement.AnyPlayer},
        {"ShootCharge", "Hold A to shoot farther", "AButton", TutorialRequirement.AnyPlayer},
        {"BallPickupTimeout", "Pick up the ball and don't shoot", null, TutorialRequirement.AnyPlayer},
        {"TimeoutShort", "You can't hold the ball forever", null, TutorialRequirement.ShortTimeout},
        {"Steal", "Dash at the ball an enemy player holds to steal", null, TutorialRequirement.AnyPlayer},
        {"resetgoal", "", null},
        {"Backboard", "Bounce the ball off the top edge", null, TutorialRequirement.AnyPlayer},
        {"TimeoutShort", "That sets the goal to your color", null, TutorialRequirement.ShortTimeout},
        {"Score", "Score into the goal when it's the same color as you", null, TutorialRequirement.Called},
        {"TimeoutShort", "You should now know how to play Kefi!", null, TutorialRequirement.ShortTimeout},
        {"Done", "Finished! Press A to play", "AButton"},
    };


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
        eventCalled = false;
    }

    bool AllCheckedIn() {
        bool all = true;
        foreach (var player in GetPlayers()) {
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
        Debug.LogFormat("Any check {0}", checkin.Values.ToList());
        foreach (var check in checkin.Values) {
            Debug.Log(check);
        }
        return checkin.Values.Any(i => i);
    }

    bool timeoutShort = false;

    bool eventCalled = false;

    bool StageDone(TutorialRequirement requirement) {
        if (requirement == TutorialRequirement.AllPlayers) {
            return AllCheckedIn();
        } else if (requirement == TutorialRequirement.AnyPlayer) {
            return AnyCheckedIn();
        } else if (requirement == TutorialRequirement.ShortTimeout) {
            return timeoutShort;
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

    void Start() {
        tutorialCanvas = GameObject.Find("TutorialCanvas");
        tutorialCanvas.SetActive(true);
        tutorialText = tutorialCanvas.FindComponent<Text>("TutorialText");
        tutorialImage = tutorialCanvas.FindComponent<Image>("TutorialImage");
        tutorialReadyText = GameObject.FindWithTag("TutorialReadyText").GetComponent<Text>();
        Debug.Log(tutorialReadyText);
        if (runTutorial) {
            StartCoroutine(Tutorial());
        }
    }

    void StartShortTimeout() {
        this.TimeDelayCall(() => timeoutShort = true, 4f);
    }

    void SetTutorialText(string text) {
        tutorialText.text = text;
    }

    void SetTutorialImage(string imagePath) {
        Debug.Log("setting tutorial image");
        if (imagePath != null) {
            var image = Resources.Load<Sprite>(string.Format("TutorialImages/{0}", imagePath));
            Debug.Log(image);
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
            SetDisplayToInfo(tutorialStage);
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
        TutorialFinished();
    }

    void TutorialFinished() {
        runTutorial = false;
        SceneStateController.instance.Load(Scene.Court);
    }

}
