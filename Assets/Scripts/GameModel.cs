using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using UtilityExtensions;


public class GameModel : MonoBehaviour {

    public static GameModel instance;
    public ScoreDisplayer scoreDisplayer;
    public NamedColor[] teamColors;
    public TeamManager[] teams { get; set; }
    public int scoreMax = 7;
    // public GameEndController end_controller {get; set;}
    public float matchLength = 5f;
    public NotificationCenter nc;
    public bool gameOver {get; private set;} = false;
    public TeamManager winner {get; private set;} = null;
    public GameObject meta;
    public float pauseAfterGoalScore = 3f;
    public float pauseAfterReset = 2f;
    public bool staticGoals = false;

    public bool pushAwayOtherPlayers = false;
    public float blowbackRadius = 3f;
    public float blowbackSpeed = 10f;
    public float blowbackStunTime = 0.1f;
    public GameObject blowbackPrefab;
    public float slowMoFactor = 0.4f;

    public Callback OnGameOver = delegate{};

    string[] countdownSoundNames = new string[]
    {"ten", "nine", "eight", "seven", "six", "five", "four", "three", "two", "one"};

    float matchLengthSeconds;
    IntCallback NextTeamAssignmentIndex;
    Ball ball;
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
        gameOver = false;
        InitializeTeams();
        matchLengthSeconds = 60 * matchLength;
        if (!PlayerTutorial.runTutorial) {
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
        scoreDisplayer?.StartMatchLengthUpdate(matchLengthSeconds);
        ball = GameObject.FindObjectOfType<Ball>();
        goal = GameObject.FindObjectOfType<Goal>();
        scoreDisplayer.StartMatchLengthUpdate(matchLengthSeconds);
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
    }

    IEnumerator EndGameCountdown() {
        foreach (var count in countdownSoundNames) {
            AudioManager.Play("countdown/" + count);
            yield return new WaitForSeconds(1f);
        }
        EndGame();
    }

    void EndGame() {
        Debug.Log("game over");
        winner = teams.Aggregate(
            (bestSoFar, next) => {
                if (bestSoFar == null || bestSoFar.score == next.score) {
                    return null;
                }
                return next.score > bestSoFar.score ? next : bestSoFar;
            });
        Debug.Log("Calling OnGameOver");
        gameOver = true;
        OnGameOver();
    }


    public TeamManager GetTeamAssignment(Player caller) {
        // var assignedTeam = teams[NextTeamAssignmentIndex()];
        var assignedTeam = teams[int.Parse(caller.name.Substring(6)) % 2];
        assignedTeam.AddTeamMember(caller);
        return assignedTeam;
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
            if (staticGoals) {
                goals[i].SetTeam(teams[i]);
            }
        }
        NextTeamAssignmentIndex = Utility.ModCycle(0, teams.Length);
    }

    public void GoalScoredForTeam(TeamManager scored) {
        Debug.Log(ball);
        ball.HandleGoalScore(scored.teamColor);
        goal?.StopTeamSwitching();
        foreach (var team in teams) {
            if ((Color)team.teamColor == scored.teamColor) {
                team.IncrementScore();
            } else {
                team.MakeInvisibleAfterGoal();
            }
        }
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
            }
        }
    }

    List<Player> GetPlayers() {
        var result = new List<Player>();
        foreach (var team in teams) {
            result.AddRange(team.teamMembers);
        }
        return result;
    }

    public void SlowMo() {
        Utility.ChangeTimeScale(slowMoFactor);
        foreach (var player in GetPlayers()) {
            player.GetComponent<PlayerMovement>().instantRotation = false;
        }
    }

    public void ResetSlowMo() {
        Utility.ChangeTimeScale(1);
        foreach (var player in GetPlayers()) {
            player.GetComponent<PlayerMovement>().instantRotation = true;
        }
    }
}
