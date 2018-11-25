using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UtilityExtensions;


public class GameManager : MonoBehaviour
{

    public static bool playerTeamsAlreadySelected = false;
    public static Dictionary<int, int> playerTeamAssignments = new Dictionary<int, int>();
    public static bool cheatForcePlayerAssignment = false;
    public static GameManager instance;
    public ScoreDisplayer scoreDisplayer;
    public NamedColor[] teamColors;
    public List<TeamManager> teams { get; set; }
    public TeamResourceManager neutralResources;
    // public GameEndController end_controller {get; set;}
    public float matchLength = 5f;
    public NotificationManager notificationManager;
    public bool gameOver { get; private set; } = false;
    public TeamManager winner { get; private set; } = null;
    public GameObject meta;
    public float pauseAfterGoalScore = 3f;
    public float pauseAfterReset = 2f;
    public List<Player> players = new List<Player>();

    [SerializeField]
    private GameSettings gameSettings;
    public GameSettings Settings { get { return gameSettings; } }

    public GameObject blowbackPrefab;

    public enum WinCondition
    {
        Time, FirstToX, TennisRules
    }

    public WinCondition winCondition = WinCondition.TennisRules;

    public Callback OnGameOver = delegate { };

    public BackgroundScroller backgroundScroller;
    private string[] countdownSoundNames = new string[]
    {"ten", "nine", "eight", "seven", "six", "five", "four", "three", "two", "one"};
    private float matchLengthSeconds;

    private Ball ball
    {
        get
        {
            return GameObject.FindObjectOfType<Ball>();
        }
    }
    public Goal goal
    {
        get
        {
            return GameObject.FindObjectOfType<Goal>();
        }
    }

    private CameraShake cameraShake;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            Initialization();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialization()
    {
        if (!PlayerTutorial.runTutorial && !playerTeamsAlreadySelected)
        {
            cheatForcePlayerAssignment = true;
        }
        gameOver = false;
        InitializeTeams();
        matchLengthSeconds = 60 * matchLength;
        if (!PlayerTutorial.runTutorial && winCondition == WinCondition.Time)
        {
            this.TimeDelayCall(() => StartCoroutine(EndGameCountdown()), matchLengthSeconds - (countdownSoundNames.Length + 1));
        }
        notificationManager = new NotificationManager();
    }

    private void BlowBack(Player player)
    {
        // TODO dkonik: This shouldn't be instantiated every time, it should be reused
        Utility.BlowbackFromPlayer(player.gameObject, Settings.BlowbackRadius, Settings.BlowbackSpeed, false,
                                   Settings.BlowbackStunTime);
        GameObject.Instantiate(blowbackPrefab, player.transform.position, player.transform.rotation);
    }

    private void Start()
    {
        SceneStateManager.instance.UnPauseTime();
        cameraShake = GameObject.FindObjectOfType<CameraShake>();
        if (winCondition == WinCondition.Time)
        {
            scoreDisplayer.StartMatchLengthUpdate(matchLengthSeconds);
        }
        // if (pushAwayOtherPlayers) {
        //     nc.CallOnStateStart(State.Posession, BlowBack);
        // }
        meta = SceneStateManager.instance.gameObject;
        if (meta == null)
        {
            Debug.LogWarning("Meta object is null!!!!");
        }

        // Set up countdown messaging through nc (3-2-1-GO at beginning of scene)
        if (!PlayerTutorial.runTutorial && SceneManager.GetActiveScene().name == "court")
        {
            notificationManager.CallOnMessage(Message.CountdownFinished, StartGameAfterBallAnimation);
            this.FrameDelayCall(
                () => { foreach (TeamManager team in teams) { team.ResetTeam(); } },
                3);
        }
    }

    private IEnumerator EndGameCountdown()
    {
        foreach (string count in countdownSoundNames)
        {
            AudioManager.Play("countdown/" + count);
            yield return new WaitForSeconds(1f);
        }
        EndGame();
    }

    private TeamManager TopTeam()
    {
        return teams.Aggregate(
            (bestSoFar, next) =>
            {
                if (bestSoFar == null || bestSoFar.score == next.score)
                {
                    return null;
                }
                return next.score > bestSoFar.score ? next : bestSoFar;
            });
    }

    private void EndGame()
    {
        winner = TopTeam();
        gameOver = true;
        OnGameOver();
    }


    public TeamManager GetTeamAssignment(Player caller)
    {
        if (GameManager.playerTeamsAlreadySelected)
        {
            return teams[playerTeamAssignments[caller.playerNumber]];
        }
        else if (GameManager.cheatForcePlayerAssignment)
        {
            return teams[caller.playerNumber % teams.Count];
        }
        return null;
    }

    private void InitializeTeams()
    {
        teams = new List<TeamManager>();
        neutralResources = new TeamResourceManager(null);

        for (int i = 0; i < teamColors.Length; ++i)
        {
            // Add 1 so we get Team 1 and Team 2
            teams.Add(new TeamManager(i + 1, teamColors[i]));
        }
    }

    private void ScoreChanged()
    {
        if (winCondition == WinCondition.FirstToX || winCondition == WinCondition.TennisRules)
        {
            CheckForWinner();
        }
    }

    private void CheckForWinner()
    {
        TeamManager topTeam = TopTeam();
        if (topTeam != null && topTeam.score >= Settings.WinningScore)
        {
            if (winCondition == WinCondition.TennisRules)
            {
                int secondBestScore =
                    (from team in teams
                     where team != topTeam
                     select team.score).Max();
                if (Mathf.Abs(secondBestScore - topTeam.score) >= Settings.RequiredWinMargin)
                {
                    EndGame();
                }
            }
            else if (winCondition == WinCondition.FirstToX)
            {
                EndGame();
            }
        }
    }

    public int AmountOneTeamAhead()
    {
        // Returns the amount the winning team is ahead by
        Debug.Assert(teams.Count >= 2);
        return Mathf.Abs(teams[0].score - teams[1].score);
    }

    public void GoalScoredForTeam(TeamManager scored)
    {
        ball.HandleGoalScore(scored.teamColor);
        goal?.StopTeamSwitching();
        foreach (TeamManager team in teams)
        {
            if ((Color)team.teamColor == scored.teamColor)
            {
                team.IncrementScore();
                TeamManager winningTeam = TopTeam();
                if (winningTeam != null)
                {
                    backgroundScroller.SetBackground(winningTeam.resources);
                }
                else
                {
                    backgroundScroller.SetBackground(neutralResources);
                }
                ScoreChanged();
            }
            else
            {
                team.MakeInvisibleAfterGoal();
            }
        }
        if (!TutorialLiveClips.runningLiveClips)
        {
            UtilityExtensionsContainer.TimeDelayCall(
                this, ResetGameAfterGoal, pauseAfterGoalScore);
        }
        cameraShake.shakeAmount = Settings.GoalShakeAmount;
        cameraShake.shakeDuration = Settings.GoalShakeDuration;
    }

    private void ResetGameAfterGoal()
    {
        if (gameOver)
        {
            return;
        }
        foreach (TeamManager team in teams)
        {
            team.ResetTeam();
        }
        ball.ResetBall(pauseAfterReset);
        notificationManager.NotifyMessage(Message.StartCountdown, this);
        GameObject.FindObjectOfType<RoundStartBlocker>()?.Reset();
        foreach (TronWall wall in GameObject.FindObjectsOfType<TronWall>())
        {
            wall.KillSelf();
        }

        goal?.SwitchToNextTeam(false);
        goal?.ResetNeutral();

        // Reset music.
        StartCoroutine(PitchShifter(1.0f, Settings.PitchShiftTime));
    }

    private void StartGameAfterBallAnimation()
    {
        // goal?.RestartTeamSwitching(); // <-- (Empty function body)
        foreach (TeamManager team in teams)
        {
            team.BeginMovement();
        }


    }
    public void GoalScoredOnTeam(TeamManager scoredOn)
    {
        foreach (TeamManager team in teams)
        {
            if ((Color)team.teamColor != scoredOn.teamColor)
            {
                team.IncrementScore();
                ScoreChanged();
            }
        }
    }

    public List<Player> GetPlayersWithTeams()
    {
        List<Player> result = new List<Player>();
        foreach (TeamManager team in teams)
        {
            result.AddRange(team.teamMembers);
        }
        return result;
    }

    public List<Player> GetAllPlayers()
    {
        return players;
    }

    public List<Player> GetHumanPlayers()
    {
        return players.Where(player => player.playerNumber >= 0).ToList();
    }

    private IEnumerator PitchShifter(float target, float time)
    {
        AudioSource backgroundMusic = GameObject.Find("BGM")?.GetComponent<AudioSource>();

        if (backgroundMusic == null) yield break;

        float start = backgroundMusic.pitch;
        float t = 0.0f;

        while (backgroundMusic.pitch != target && t <= time)
        {
            t += Time.deltaTime;
            backgroundMusic.pitch = Mathf.Lerp(start, target, t / time);
            yield return null;
        }

        backgroundMusic.pitch = target;
    }

    public bool IsSlowMo()
    {
        return slowMoCount > 0;
    }

    private int slowMoCount = 0;
    public void SlowMo()
    {
        Utility.ChangeTimeScale(Settings.SlowMoFactor);
        foreach (Player player in GetAllPlayers())
        {
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                movement.instantRotation = false;
            }
        }
        // Ensure slowMo doesn't stop until ALL balls are dropped
        slowMoCount += 1;
        notificationManager.NotifyMessage(Message.SlowMoEntered, this);
        if (!TutorialLiveClips.runningLiveClips)
        {
            StartCoroutine(PitchShifter(Settings.SlowedPitch, Settings.PitchShiftTime));
        }
    }

    public void ResetSlowMo()
    {
        // Ensure slowMo doesn't stop until ALL balls are dropped
        slowMoCount -= 1;
        if (slowMoCount == 0)
        {
            Utility.ChangeTimeScale(1);
            IEnumerable<PlayerMovement> movements = (from player in GetAllPlayers()
                                                     where player.GetComponent<PlayerMovement>() != null
                                                     select player.GetComponent<PlayerMovement>());
            foreach (PlayerMovement movement in movements)
            {
                movement.instantRotation = true;
            }

            // Pitch-shift BGM back to normal.
            if (!TutorialLiveClips.runningLiveClips)
            {
                StartCoroutine(PitchShifter(1.0f, Settings.PitchShiftTime));
            }
            notificationManager.NotifyMessage(Message.SlowMoExited, this);
        }
    }

    public Player GetPlayerFromNumber(int playerNumber) {
        return players.Where(p => p.playerNumber == playerNumber).First();
    }

    public void FlashScreen(float flashLength = 0.1f, Color? flashColor = null)
    {
        if (flashColor == null)
        {
            flashColor = Color.white;
        }
        drawFlash = true;
        this.flashColor = flashColor.Value;
        this.TimeDelayCall(() => drawFlash = false, flashLength);
    }

    private bool drawFlash = false;
    private Color flashColor = Color.white;

    private void OnGUI()
    {
        if (drawFlash)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, flashColor);
            texture.Apply();
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), texture);
        }
    }
}
