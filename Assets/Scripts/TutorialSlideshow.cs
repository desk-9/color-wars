using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UtilityExtensions;

public class TutorialSlideshow : MonoBehaviour {
    public static TutorialSlideshow instance;

    string tutorialVideoDirectory = "TutorialClips";
    // Slide pairs are of the form (string for the info text, filename of video)
    List<Tuple<string, string>> slides = new List<Tuple<string, string>>() {
        {"TOUCH the ball to pick it up", "01-touch-ball-to-pick-up"},
        {"Press A to SHOOT the ball", "02-press-a-to-shoot-ball"},
        {"PASS to your teammate to CHARGE the ball with your color", "03-pass-to-teammate-to-charge-ball"},
        {"SHOOT at the goal when the ball is your color", "04-shoot-to-score-when-ball-is-your-color"},
        {"DASH at the ball when an enemy has it to STEAL", "05-dash-to-steal"},
        {"Hold B to lay a WALL", "06-lay-wall"},
        {"DASH at a wall to BREAK it", "07-dash-at-wall-to-break-it"},
        {"BREAK a wall while a player is placing it to STUN them", "08-break-while-laying-to-stun"},
        {"Score 3 times to win!", "09-score-3-times-to-win"},
        {"Press A to start the game!", "10-this-is-seriously-just-a-2-second-video-of-eigengrau-oh-god-i-hate-myself"},
    };

    List<VideoClip> videos = new List<VideoClip>();

    Canvas tutorialCanvas;
    Text infoText;
    Text readyText;
    VideoPlayer videoPlayer;
    UIVideo updater;
    Dictionary<GameObject, bool> checkin = new Dictionary<GameObject, bool>();
    bool nextSlideForceCheat = false;

    void Awake() {
        if (instance == null) {
            instance = this;
        } else {
            Destroy(this);
        }
    }

    void Start() {
        tutorialCanvas = GameObject.Find("TutorialCanvas").GetComponent<Canvas>();
        if (tutorialCanvas != null) {
            infoText = tutorialCanvas.FindComponent<Text>("Info");
            videoPlayer = tutorialCanvas.FindComponent<VideoPlayer>("TutorialVideo");
            updater = videoPlayer.GetComponent<UIVideo>();
            readyText = tutorialCanvas.FindComponent<Text>("ReadyText");
            StartCoroutine(Slideshow());
        }
    }

    void StartListeningForPlayers() {
        GameModel.instance.nc.CallOnMessageWithSender(
            Message.PlayerPressedA, CheckinPlayer);
        GameModel.instance.nc.CallOnMessage(
            Message.PlayerPressedLeftBumper, () => nextSlideForceCheat = true);
    }

    List<GameObject> GetPlayers() {
        return (from player in GameModel.instance.GetAllPlayers()
                select player.gameObject).ToList();
    }

    void ResetCheckin() {
        nextSlideForceCheat = false;
        foreach (var player in GetPlayers()) {
            checkin[player.gameObject] = false;
        }
        readyText.text = string.Format("Press A to continue ({0}/{1})",
                                       NumberCheckedIn(), GetPlayers().Count);
    }

    void CheckinPlayer(object potentialPlayer) {
        var player = potentialPlayer as GameObject;
        if (player != null) {
            checkin[player] = true;
        }
        readyText.text = string.Format("Press A to continue ({0}/{1})",
                                       NumberCheckedIn(), GetPlayers().Count);
    }

    int NumberCheckedIn() {
        return GetPlayers().Count(player => checkin[player]);
    }

    bool AllCheckedIn() {
        var allPlayers = (from player in GetPlayers() select checkin[player]).All(x => x);
        return allPlayers || nextSlideForceCheat;
    }

    IEnumerator Slideshow() {
        StartListeningForPlayers();
        yield return null;

        foreach (var slide in slides) {
            ResetCheckin();
            var slideText = slide.Item1;
            VideoClip slideVideo;
            if (slide.Item2 == null) {
                slideVideo = null;
            } else {
                var slideVideoPath = string.Format("{0}/{1}", tutorialVideoDirectory, slide.Item2);
                slideVideo = Resources.Load<VideoClip>(slideVideoPath);
                if (slideVideo == null) {
                    Debug.LogError("Video clip not found!");
                    continue;
                }
            }
            infoText.text = slideText;
            videoPlayer.clip = slideVideo;
            updater.StartVideoUpdate();
            while (!AllCheckedIn()) {
                yield return null;
            }
            videoPlayer.Stop();
            yield return null;
            yield return null;

            AudioManager.instance.Beep.Play();
        }
        yield return new WaitForSeconds(0.5f);
        PlayerTutorial.runTutorial = false;
        SceneStateController.instance.Load(Scene.Court);
    }
}
