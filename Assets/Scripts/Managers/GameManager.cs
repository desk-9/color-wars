using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UtilityExtensions;


public class GameManager : MonoBehaviour
{
    #region Managers
    public NotificationManager NotificationManager { get; set; }
    public PossessionManager PossessionManager { get; set; }
    public SlowMoManager SlowMoManager { get; set; }
    #endregion

    public static bool playerTeamsAlreadySelected = false;
    public static Dictionary<int, int> playerTeamAssignments = new Dictionary<int, int>();
    public static bool cheatForcePlayerAssignment = false;
    public static GameManager instance;
    public ScoreDisplayer scoreDisplayer;
    public NamedColor[] teamColors;
    public List<TeamManager> Teams { get; set; }
    public TeamResourceManager neutralResources;
    // public GameEndController end_controller {get; set;}
    public float matchLength = 5f;
    
    public bool gameOver { get; private set; } = false;
    public TeamManager Winner { get; private set; } = null;
    public GameObject meta;
    public float pauseAfterGoalScore = 3f;
    public float pauseAfterReset = 2f;
    public List<Player> players = new List<Player>();

    [SerializeField]
    private GameSettings gameSettings;
    public static GameSettings Settings { get
        {
            instance.ThrowIfNull("Instance was null when trying to acquire GameSettings");
            return instance.gameSettings;
        }
    }

    public GameObject blowbackPrefab;

    public enum WinCondition
    {
        Time, FirstToX, TennisRules
    }

    public WinCondition winCondition = WinCondition.TennisRules;

    public Callback OnGameOver = delegate { };

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
        NotificationManager = new NotificationManager();
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
        PossessionManager = this.EnsureComponent<PossessionManager>();
        SlowMoManager = this.EnsureComponent<SlowMoManager>();
    }

    private void Start()
    {
        SceneStateManager.instance.UnPauseTime();
        cameraShake = GameObject.FindObjectOfType<CameraShake>();
        if (winCondition == WinCondition.Time)
        {
            scoreDisplayer.StartMatchLengthUpdate(matchLengthSeconds);
        }

        meta = SceneStateManager.instance.gameObject;
        if (meta == null)
        {
            Debug.LogWarning("Meta object is null!!!!");
        }

        // The reason we subscribe to both goal scored and score changed is because when goal scored fires,
        // the appropriate 
        NotificationManager.CallOnMessage(Message.ScoreChanged, HandleScoreChange);
        NotificationManager.CallOnMessage(Message.GoalScored, HandleGoalScored);
    }

    private void HandleGoalScored()
    {
        cameraShake.shakeAmount = Settings.GoalShakeAmount;
        cameraShake.shakeDuration = Settings.GoalShakeDuration;
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

    public TeamManager GetWinningTeam()
    {
        return Teams.Aggregate(
            (bestSoFar, next) =>
            {
                if (bestSoFar == null || bestSoFar.Score == next.Score)
                {
                    return null;
                }
                return next.Score > bestSoFar.Score ? next : bestSoFar;
            });
    }

    private void EndGame()
    {
        Winner = GetWinningTeam();
        gameOver = true;
        OnGameOver();
    }


    public TeamManager GetTeamAssignment(Player caller)
    {
        if (GameManager.playerTeamsAlreadySelected)
        {
            return Teams[playerTeamAssignments[caller.playerNumber]];
        }
        else if (GameManager.cheatForcePlayerAssignment)
        {
            return Teams[caller.playerNumber % Teams.Count];
        }
        return null;
    }

    private void InitializeTeams()
    {
        Teams = new List<TeamManager>();
        neutralResources = new TeamResourceManager(null);

        for (int i = 0; i < teamColors.Length; ++i)
        {
            // Add 1 so we get Team 1 and Team 2
            Teams.Add(new TeamManager(i + 1, teamColors[i]));
        }
    }

    private void HandleScoreChange()
    {
        if (winCondition == WinCondition.FirstToX || winCondition == WinCondition.TennisRules)
        {
            CheckForWinner();
        }
    }

    private void CheckForWinner()
    {
        TeamManager topTeam = GetWinningTeam();
        if (topTeam != null && topTeam.Score >= Settings.WinningScore)
        {
            if (winCondition == WinCondition.TennisRules)
            {
                int secondBestScore =
                    (from team in Teams
                     where team != topTeam
                     select team.Score).Max();
                if (Mathf.Abs(secondBestScore - topTeam.Score) >= Settings.RequiredWinMargin)
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

    private void ResetGameAfterGoal()
    {
        // TODO dkonik: This should just fire the event and everything else should
        // take care of that
        if (gameOver)
        {
            return;
        }
        NotificationManager.NotifyMessage(Message.Reset, this);

        ball.ResetBall(pauseAfterReset);
        NotificationManager.NotifyMessage(Message.StartCountdown, this);
        GameObject.FindObjectOfType<RoundStartBlocker>()?.Reset();
        foreach (TronWall wall in GameObject.FindObjectsOfType<TronWall>())
        {
            wall.KillSelf();
        }


        // Reset music.
        StartCoroutine(PitchShifter(1.0f, Settings.PitchShiftTime));
    }

    public List<Player> GetPlayersWithTeams()
    {
        List<Player> result = new List<Player>();
        foreach (TeamManager team in Teams)
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
