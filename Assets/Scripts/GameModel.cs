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
    public TeamManager[] teams { get; set; }
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
    public float PitchShiftTime = 0.2f;
    public float SlowedPitch = 0.9f;

    public int winningScore = 5;
    public int requiredWinMargin = 2;

    public TeamManager iceTeam;
    public TeamManager fireTeam;

    public enum WinCondition {
        Time, FirstToX, TennisRules
    }

    public WinCondition winCondition = WinCondition.TennisRules;

    public Callback OnGameOver = delegate{};

    string[] countdownSoundNames = new string[]
    {"ten", "nine", "eight", "seven", "six", "five", "four", "three", "two", "one"};

    float matchLengthSeconds;
    IntCallback NextTeamAssignmentIndex;
    Ball ball;
    ThermometerFill thermometerFill;
    public Goal goal;

    void Awake() {
        Debug.Log(PlayerTutorial.runTutorial);
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
        Utility.BlowbackFromPlayer(player.gameObject, blowbackRadius, blowbackSpeed, true,
                                   blowbackStunTime);
        GameObject.Instantiate(blowbackPrefab, player.transform.position, player.transform.rotation);
    }

    void Start() {
        SceneStateController.instance.UnPauseTime();
        Debug.Log(Time.timeScale);
        ball = GameObject.FindObjectOfType<Ball>();
        goal = GameObject.FindObjectOfType<Goal>();
        if (winCondition == WinCondition.Time) {
            scoreDisplayer.StartMatchLengthUpdate(matchLengthSeconds);
        }
        if (pushAwayOtherPlayers) {
            nc.CallOnStateStart(State.Posession, BlowBack);
        }
        meta = SceneStateController.instance.gameObject;
        if (meta == null) {
            Debug.LogWarning("Meta object is null!!!!");
        }
        foreach (var i in PlayerInputManager.instance.devices) {
            Debug.LogFormat("{0}: {1}", i.Key.SortOrder, i.Value);
        }
        thermometerFill = Object.FindObjectOfType<ThermometerFill>();
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
        Debug.Log("game over");
        winner = TopTeam();
        Debug.Log("Calling OnGameOver");
        gameOver = true;
        OnGameOver();
    }


    public TeamManager GetTeamAssignment(Player caller) {
        if (GameModel.playerTeamsAlreadySelected) {
            return teams[playerTeamAssignments[caller.playerNumber]];
        } else if (GameModel.cheatForcePlayerAssignment) {
            return teams[NextTeamAssignmentIndex()];
        }
        return null;
    }

    void InitializeTeams() {
        teams = new TeamManager[teamColors.Length];
        var goals = FindObjectsOfType<NewGoal>();

        if (staticGoals) {
            Debug.Assert(goals.Length == teams.Length);
        }

        for (int i = 0; i < teamColors.Length; ++i) {
            // Add 1 so we get Team 1 and Team 2
            teams[i] = new TeamManager(i + 1, teamColors[i]);
            if (teamColors[i].name == "Ice") {
                iceTeam = teams[i];
            } else if (teamColors[i].name == "Fire") {
                fireTeam = teams[i];
            }
            if (staticGoals) {
                goals[i].SetTeam(teams[i]);
            }
        }
        NextTeamAssignmentIndex = Utility.ModCycle(0, teams.Length);
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

    public void GoalScoredForTeam(TeamManager scored) {
        ball.HandleGoalScore(scored.teamColor);
        goal?.StopTeamSwitching();
        foreach (var team in teams) {
            if ((Color)team.teamColor == scored.teamColor) {
                team.IncrementScore();
                ScoreChanged();
            } else {
                team.MakeInvisibleAfterGoal();
            }
        }
        foreach(var wall in GameObject.FindObjectsOfType<TronWall>()) {
            wall.KillSelf();
        }
        thermometerFill.UpdateScore();
        UtilityExtensionsContainer.TimeDelayCall(this, ResetGameAfterGoal, pauseAfterGoalScore);
    }

    void ResetGameAfterGoal() {
        foreach (var team in teams) {
            team.ResetTeam();
        }
        ball.ResetBall(pauseAfterReset);
        UtilityExtensionsContainer.TimeDelayCall(this, StartGameAfterBallAnimation, pauseAfterReset);

        goal?.SwitchToNextTeam(false);
        goal?.ResetNeutral();
    }

    void StartGameAfterBallAnimation() {
        goal?.RestartTeamSwitching();
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
        StartCoroutine(PitchShifter(SlowedPitch, PitchShiftTime));
    }

    public void ResetSlowMo() {
        // Ensure slowMo doesn't stop until ALL balls are dropped
        slowMoCount -= 1;
        if (slowMoCount == 0) {
            Utility.ChangeTimeScale(1);
            foreach (var player in GetPlayersWithTeams()) {
                player.GetComponent<PlayerMovement>().instantRotation = true;
            }

            // Pitch-shift BGM back to normal.
            StartCoroutine(PitchShifter(1.0f, PitchShiftTime));
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
