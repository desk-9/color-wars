using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using UtilityExtensions;


public class GameModel : MonoBehaviour {

    public static bool playerTeamsAlreadySelected = false;
    public static Dictionary<int, int> playerTeamAssignments = new Dictionary<int, int>();
    public static bool cheatForcePlayerAssignment = false;
    public static GameModel instance;
    public ScoreDisplayer scoreDisplayer;
    public NamedColor[] teamColors;
    public List<TeamManager> teams { get; set; }
    public TeamResourceManager neutralResources;
    // public GameEndController end_controller {get; set;}
    public float matchLength = 5f;
    public NotificationCenter nc;
    public bool gameOver {get; private set;} = false;
    public TeamManager winner {get; private set;} = null;
    public GameObject meta;
    public float pauseAfterGoalScore = 3f;
    public float pauseAfterReset = 2f;
    public bool staticGoals = false;
    public List<Player> players = new List<Player>();

    public bool pushAwayOtherPlayers = false;
    public float blowbackRadius = 3f;
    public float blowbackSpeed = 10f;
    public float blowbackStunTime = 0.1f;
    public GameObject blowbackPrefab;
    public float slowMoFactor = 0.4f;
    public float PitchShiftTime = 0.3f;
    public float SlowedPitch = 0.5f;
    public float goalShakeAmount = 1.5f;
    public float goalShakeDuration = .4f;

    public bool respectSoundEffectSlowMo = true;

    public int winningScore = 5;
    public int requiredWinMargin = 2;

    public enum WinCondition {
        Time, FirstToX, TennisRules
    }

    public WinCondition winCondition = WinCondition.TennisRules;

    public Callback OnGameOver = delegate{};

    public BackgroundScroller backgroundScroller;

    string[] countdownSoundNames = new string[]
    {"ten", "nine", "eight", "seven", "six", "five", "four", "three", "two", "one"};

    float matchLengthSeconds;
    Ball ball {
        get {
            return GameObject.FindObjectOfType<Ball>();
        }
    }
    public Goal goal {
        get {
            return GameObject.FindObjectOfType<Goal>();
        }
    }
    CameraShake cameraShake;

    void Awake() {
        if (instance == null) {
            instance = this;
            Initialization();
        } else {
            Destroy(gameObject);
        }
    }

    void Initialization() {
        if (!PlayerTutorial.runTutorial && !playerTeamsAlreadySelected) {
            cheatForcePlayerAssignment = true;
        }
        gameOver = false;
        InitializeTeams();
        matchLengthSeconds = 60 * matchLength;
        if (!PlayerTutorial.runTutorial && winCondition == WinCondition.Time) {
            this.TimeDelayCall(() => StartCoroutine(EndGameCountdown()), matchLengthSeconds - (countdownSoundNames.Length + 1));
        }
        nc = new NotificationCenter();
    }

    void BlowBack(Player player) {
        Utility.BlowbackFromPlayer(player.gameObject, blowbackRadius, blowbackSpeed, false,
                                   blowbackStunTime);
        GameObject.Instantiate(blowbackPrefab, player.transform.position, player.transform.rotation);
    }

    void Start() {
        SceneStateController.instance.UnPauseTime();
        cameraShake = GameObject.FindObjectOfType<CameraShake>();
        if (winCondition == WinCondition.Time) {
            scoreDisplayer.StartMatchLengthUpdate(matchLengthSeconds);
        }
        // if (pushAwayOtherPlayers) {
        //     nc.CallOnStateStart(State.Posession, BlowBack);
        // }
        meta = SceneStateController.instance.gameObject;
        if (meta == null) {
            Debug.LogWarning("Meta object is null!!!!");
        }

        // Set up countdown messaging through nc (3-2-1-GO at beginning of scene)
        if (!PlayerTutorial.runTutorial && SceneManager.GetActiveScene().name == "court") {
            nc.CallOnMessage(Message.CountdownFinished, StartGameAfterBallAnimation);
            this.FrameDelayCall(
                () => {foreach (var team in teams) {team.ResetTeam();}},
                3);
        }
    }

    IEnumerator EndGameCountdown() {
        foreach (var count in countdownSoundNames) {
            AudioManager.Play("countdown/" + count);
            yield return new WaitForSeconds(1f);
        }
        EndGame();
    }

    TeamManager TopTeam() {
        return teams.Aggregate(
            (bestSoFar, next) => {
                if (bestSoFar == null || bestSoFar.score == next.score) {
                    return null;
                }
                return next.score > bestSoFar.score ? next : bestSoFar;
            });
    }

    void EndGame() {
        winner = TopTeam();
        gameOver = true;
        OnGameOver();
    }


    public TeamManager GetTeamAssignment(Player caller) {
        if (GameModel.playerTeamsAlreadySelected) {
            return teams[playerTeamAssignments[caller.playerNumber]];
        } else if (GameModel.cheatForcePlayerAssignment) {
            return teams[caller.playerNumber % teams.Count];
        }
        return null;
    }

    void InitializeTeams() {
        teams = new List<TeamManager>();
        neutralResources = new TeamResourceManager(null);
        var goals = FindObjectsOfType<NewGoal>();

        for (int i = 0; i < teamColors.Length; ++i) {
            // Add 1 so we get Team 1 and Team 2
            teams.Add(new TeamManager(i + 1, teamColors[i]));
            if (staticGoals) {
                goals[i].SetTeam(teams[i]);
            }
        }
    }

    void ScoreChanged() {
        if (winCondition == WinCondition.FirstToX || winCondition == WinCondition.TennisRules) {
            CheckForWinner();
        }
    }

    void CheckForWinner() {
        var topTeam = TopTeam();
        if (topTeam != null && topTeam.score >= winningScore) {
            if (winCondition == WinCondition.TennisRules) {
                var secondBestScore =
                    (from team in teams
                     where team != topTeam
                     select team.score).Max();
                if (Mathf.Abs(secondBestScore - topTeam.score) >= requiredWinMargin) {
                    EndGame();
                }
            } else if (winCondition == WinCondition.FirstToX) {
                EndGame();
            }
        }
    }

    public int AmountOneTeamAhead() {
        // Returns the amount the winning team is ahead by
        Debug.Assert(teams.Count >= 2);
        return Mathf.Abs(teams[0].score - teams[1].score);
    }

    public void GoalScoredForTeam(TeamManager scored) {
        ball.HandleGoalScore(scored.teamColor);
        goal?.StopTeamSwitching();
        foreach (var team in teams) {
            if ((Color)team.teamColor == scored.teamColor) {
                team.IncrementScore();
                var winningTeam = TopTeam();
                if (winningTeam != null) {
                    backgroundScroller.SetBackground(winningTeam.resources);
                } else {
                    backgroundScroller.SetBackground(neutralResources);
                }
                ScoreChanged();
            } else {
                team.MakeInvisibleAfterGoal();
            }
        }
        if (!TutorialLiveClips.runningLiveClips) {
            UtilityExtensionsContainer.TimeDelayCall(
                this, ResetGameAfterGoal, pauseAfterGoalScore);
        }
        cameraShake.shakeAmount = goalShakeAmount;
        cameraShake.shakeDuration = goalShakeDuration;
    }

    void ResetGameAfterGoal() {
        if (gameOver) {
            return;
        }
        foreach (var team in teams) {
            team.ResetTeam();
        }
        ball.ResetBall(pauseAfterReset);
        nc.NotifyMessage(Message.StartCountdown, this);
        GameObject.FindObjectOfType<RoundStartBlocker>().Reset();
        foreach(var wall in GameObject.FindObjectsOfType<TronWall>()) {
            wall.KillSelf();
        }

        goal?.SwitchToNextTeam(false);
        goal?.ResetNeutral();

        // Reset music.
        StartCoroutine(PitchShifter(1.0f, PitchShiftTime));
    }

    void StartGameAfterBallAnimation() {
        // goal?.RestartTeamSwitching(); // <-- (Empty function body)
        foreach (var team in teams) {
            team.BeginMovement();
        }


    }
    public void GoalScoredOnTeam(TeamManager scoredOn) {
        foreach (var team in teams) {
            if ((Color)team.teamColor != scoredOn.teamColor) {
                team.IncrementScore();
                ScoreChanged();
            }
        }
    }

    public List<Player> GetPlayersWithTeams() {
        var result = new List<Player>();
        foreach (var team in teams) {
            result.AddRange(team.teamMembers);
        }
        return result;
    }

    public List<Player> GetAllPlayers() {
        return players;
    }

    public List<Player> GetHumanPlayers() {
        return players.Where(player => player.playerNumber >= 0).ToList();
    }

    IEnumerator PitchShifter(float target, float time) {
        var backgroundMusic = GameObject.Find("BGM")?.GetComponent<AudioSource>();

        if (backgroundMusic == null) yield break;

        var start = backgroundMusic.pitch;
        var t     = 0.0f;

        while (backgroundMusic.pitch != target && t <= time) {
            t += Time.deltaTime;
            backgroundMusic.pitch = Mathf.Lerp(start, target, t / time);
            yield return null;
        }

        backgroundMusic.pitch = target;
    }

    public bool IsSlowMo() {
        return slowMoCount > 0;
    }

    int slowMoCount = 0;
    public void SlowMo() {
        Utility.ChangeTimeScale(slowMoFactor);
        foreach (var player in GetAllPlayers()) {
            var movement = player.GetComponent<PlayerMovement>();
            if (movement != null) {
                movement.instantRotation = false;
            }
        }
        // Ensure slowMo doesn't stop until ALL balls are dropped
        slowMoCount += 1;
        nc.NotifyMessage(Message.SlowMoEntered, this);
        if (!TutorialLiveClips.runningLiveClips) {
            StartCoroutine(PitchShifter(SlowedPitch, PitchShiftTime));
        }
    }

    public void ResetSlowMo() {
        // Ensure slowMo doesn't stop until ALL balls are dropped
        slowMoCount -= 1;
        if (slowMoCount == 0) {
            Utility.ChangeTimeScale(1);
            var movements = (from player in GetAllPlayers()
                             where player.GetComponent<PlayerMovement>() != null
                             select player.GetComponent<PlayerMovement>());
            foreach (var movement in movements) {
                movement.instantRotation = true;
            }

            // Pitch-shift BGM back to normal.
            if (!TutorialLiveClips.runningLiveClips) {
                StartCoroutine(PitchShifter(1.0f, PitchShiftTime));
            }
            nc.NotifyMessage(Message.SlowMoExited, this);
        }
    }

    public void FlashScreen(float flashLength = 0.1f, Color? flashColor = null) {
        if (flashColor == null) {
            flashColor = Color.white;
        }
        drawFlash = true;
        this.flashColor = flashColor.Value;
        this.TimeDelayCall(() => drawFlash = false, flashLength);
    }

    bool drawFlash = false;
    Color flashColor = Color.white;

    void OnGUI() {
        if (drawFlash) {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, flashColor);
            texture.Apply();
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texture);
        }
    }
}
