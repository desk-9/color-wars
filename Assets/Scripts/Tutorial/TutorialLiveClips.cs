using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UtilityExtensions;

public struct SubclipInfo
{
    public string clipText;
    public float timeAdjustment;
    public SubclipInfo(string clipText = "", float timeAdjustment = 0)
    {
        this.clipText = clipText;
        this.timeAdjustment = timeAdjustment;
    }
}

public struct LiveClipInfo
{
    public string clipName;
    public List<SubclipInfo> subclipInfo;
    public float preDelay;
    public float postDelay;
    public LiveClipInfo(string clipName, List<SubclipInfo> subclipInfo,
                        float preDelay = 0, float postDelay = 0)
    {
        this.clipName = clipName;
        this.subclipInfo = subclipInfo;
        this.preDelay = preDelay;
        this.postDelay = postDelay;
    }
}

public class TutorialLiveClips : MonoBehaviour
{
    public static TutorialLiveClips instance;
    public static bool runningLiveClips = false;

    // Live Slide Format: (slideObjectName, [subsection_1_text, subsection_2_text...])
    //
    // slideObjectName is the name of a game object in the scene which contains
    // whatever game objects are relevant to this live slide. Probably a few
    // puppet players, maybe a ball.
    //
    // The subsection text is the tutorial text to be displayed by each
    // subsection of a given live slide, in order.
    private List<LiveClipInfo> liveClips = new List<LiveClipInfo>() {
        {"1-shoot-pass-and-score",
         new List<SubclipInfo>() {
                {"TOUCH the ball to pick it up", 0},
                {"SHOOT the ball with <AButton>", 0},
                {"but you must pass to fill the ball...", 0.3f},
                {"...and you can only score with a filled ball", .5f}
         }, 0, 2f},
        {"2-cant-pass-in-null-zone",
         new List<SubclipInfo>() {
                "passes don't fill the ball in the null zone",
         }},
        {"3-stealing-and-blocking",
         new List<SubclipInfo>() {
                {"DASH with <AButton> at the ball to STEAL", 0},
                "BLOCK steals with your body"
         }},
        {"4-walls",
         new List<SubclipInfo>() {
                "Hold <BButton> to lay WALLS",
                "Use WALLS to BLOCK the ball",
                "BREAK walls by DASHING"
         }}

    };
    private Canvas tutorialCanvas;
    private RichText infoText;
    private RichText readyText;
    private Dictionary<GameObject, bool> checkin = new Dictionary<GameObject, bool>();
    private bool nextSlideForceCheat = false;
    private LiveClipInfo currentClip;
    private string currentClipName;
    private int currentSubclipIndex = 0;
    private List<SubclipInfo> currentSubclips;
    private bool atLeastOneLoop = false;
    private bool clipReloadThisFrame = false;
    private PlayerCheckin ySkip;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        tutorialCanvas = GameObject.Find("TutorialCanvas").GetComponent<Canvas>();
        if (tutorialCanvas != null)
        {
            ySkip = new PlayerCheckin(() => GetPlayers(), Message.PlayerPressedY,
                                      checkoutEvent: Message.PlayerReleasedY);
            infoText = tutorialCanvas.FindComponent<RichText>("Info");
            readyText = tutorialCanvas.FindComponent<RichText>("ReadyText");
            StartCoroutine(Clips());
        }
    }

    private void StartListeningForPlayers()
    {
        GameManager.NotificationManager.CallOnMessageWithSender(
            Message.PlayerPressedX, CheckinPlayer);
        GameManager.NotificationManager.CallOnMessage(
            Message.PlayerPressedLeftBumper, () => nextSlideForceCheat = true);
    }

    private List<GameObject> GetPlayers()
    {
        return (from player in GameManager.Instance.GetHumanPlayers()
                select player.gameObject).ToList();
    }

    private void ResetCheckin()
    {
        atLeastOneLoop = false;
        nextSlideForceCheat = false;
        foreach (GameObject player in GetPlayers())
        {
            checkin[player.gameObject] = false;
        }
        readyText.text = "";
    }

    private void SetReadyText()
    {
        if (atLeastOneLoop)
        {
            readyText.text = string.Format("Press <XButton> to continue ({0}/{1})",
                                           NumberCheckedIn(), GetPlayers().Count);
        }
    }

    private void CheckinPlayer(object potentialPlayer)
    {
        if (!atLeastOneLoop)
        {
            return;
        }
        GameObject player = potentialPlayer as GameObject;
        if (player != null)
        {
            checkin[player] = true;
        }
        SetReadyText();
    }

    private int NumberCheckedIn()
    {
        return GetPlayers().Count(player => checkin[player]);
    }

    private bool AllCheckedIn()
    {
        bool allPlayers = (from player in GetPlayers() select checkin[player]).All(x => x);
        return (allPlayers && atLeastOneLoop) || nextSlideForceCheat;
    }

    private void LoadLiveClip(string clipName)
    {
        currentSubclipIndex = 0;
        SceneManager.LoadScene(clipName, LoadSceneMode.Additive);
        SetCurrentSubclip();
        //foreach (TeamManager team in GameManager.instance.teams)
        //{
        //    team.ResetScore();
        //}
    }

    private IEnumerator Clips()
    {
        runningLiveClips = true;
        StartListeningForPlayers();
        GameManager.NotificationManager.CallOnMessage(Message.RecordingFinished,
                                            () =>
                                            {
                                                if (!clipReloadThisFrame)
                                                {
                                                    ClipReload();
                                                    clipReloadThisFrame = true;
                                                    this.FrameDelayCall(() => clipReloadThisFrame = false, 3);
                                                }
                                            });
        GameManager.NotificationManager.CallOnMessage(Message.RecordingInterrupt,
                                            SubclipInterrupt);
        yield return null;
        ySkip.StartListening();
        foreach (LiveClipInfo liveClip in liveClips)
        {
            clipReloadThisFrame = false;
            currentClip = liveClip;
            ResetCheckin();
            currentClipName = liveClip.clipName;
            currentSubclips = liveClip.subclipInfo;
            yield return new WaitForSecondsRealtime(liveClip.preDelay);
            LoadLiveClip(currentClipName);
            yield return null;
            while (!AllCheckedIn() && !ySkip.AllCheckedIn())
            {
                yield return null;
            }
            yield return null;
            TransitionUtility.OneShotFadeTransition(0.3f, 0.2f);
            yield return new WaitForSecondsRealtime(0.15f);
            UnloadCurrentClip();
            yield return null;
            if (ySkip.AllCheckedIn())
            {
                break;
            }
        }
        TransitionUtility.OneShotFadeTransition(0.1f, 0.4f);
        yield return new WaitForSeconds(0.05f);
        runningLiveClips = false;
        if (ySkip.AllCheckedIn())
        {
            PlayerTutorial.SkipTutorial();
        }
        else
        {
            SceneStateManager.instance.Load(Scene.Sandbox);
        }
    }

    private void SetSubclipText(string text)
    {
        infoText.text = text;
    }

    private void SetCurrentSubclip()
    {
        if (currentSubclipIndex < currentSubclips.Count)
        {
            SubclipInfo subclip = currentSubclips[currentSubclipIndex];
            SetSubclipText(subclip.clipText);
        }
    }

    private void SubclipInterrupt()
    {
        if (currentSubclipIndex < currentSubclips.Count)
        {
            PlayerPuppet.puppetsPause = true;
            SubclipInfo subclip = currentSubclips[currentSubclipIndex];
            this.TimeDelayCall(() =>
            {
                currentSubclipIndex += 1;
                SetCurrentSubclip();
                PlayerPuppet.puppetsPause = false;
            },
                subclip.timeAdjustment);
        }
    }

    private void UnloadCurrentClip()
    {
        GameObject clipObject = GameObject.Find(currentClipName);
        if (clipObject)
        {
            Destroy(clipObject);
        }
        SceneManager.UnloadSceneAsync(currentClipName);
    }

    private void ClipReload()
    {
        string clipName = currentClipName;
        atLeastOneLoop = true;
        this.TimeDelayCall(() =>
        {
            if (currentClipName == clipName)
            {
                UnloadCurrentClip();
                this.RealtimeDelayCall(() =>
                {
                    if (currentClipName == clipName)
                    {
                        LoadLiveClip(clipName);
                        SetReadyText();
                    }
                }, 0.1f);
            }
        }, Mathf.Max(currentClip.postDelay, 0.1f));

        // reason for this value: 0.07f is slightly longer than the 0.05f delay
        // from a few lines up
        float epsilon = 0.07f;
        float delayBeforeFade = currentClip.postDelay / 4;
        float totalTransitionDuration = Mathf.Max(delayBeforeFade + epsilon, 0.1f);
        this.TimeDelayCall(
            () => TransitionUtility.OneShotFadeTransition(totalTransitionDuration * 2, totalTransitionDuration * 3),
            delayBeforeFade * .3f);
    }
}
