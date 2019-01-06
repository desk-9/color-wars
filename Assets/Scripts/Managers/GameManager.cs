using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using UtilityExtensions;


public class GameManager : MonoBehaviour
{
    #region Managers
    // Static accessors to be less tedious to access all of the time
    public static NotificationManager NotificationManager { get { return Instance.notificationManager; } }
    public static PossessionManager PossessionManager { get { return Instance.possessionManager; } }
    public static SlowMoManager SlowMoManager { get { return Instance.slowMoManager; } }

    // The actual managers
    private NotificationManager notificationManager;
    private PossessionManager possessionManager;
    private SlowMoManager slowMoManager;
    #endregion

    public static bool playerTeamsAlreadySelected = false;
    public static Dictionary<int, int> playerTeamAssignments = new Dictionary<int, int>();
    public static bool cheatForcePlayerAssignment = false;
    public static GameManager Instance;
    public ScoreDisplayer scoreDisplayer;
    public NamedColor[] teamColors;
    public List<TeamManager> Teams { get; set; }
    public TeamResourceManager neutralResources;
    public bool gameOver { get; private set; } = false;
    public TeamManager Winner { get; private set; } = null;
    public GameObject meta;


    public List<Player> players = new List<Player>();

    [SerializeField]
    private GameSettings gameSettings;
    public static GameSettings Settings { get
        {
            Instance.ThrowIfNull("Instance was null when trying to acquire GameSettings");
            return Instance.gameSettings;
        }
    }

    public GameObject blowbackPrefab;

    public Callback OnGameOver = delegate { };

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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Initialization();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialization()
    {
        notificationManager = new NotificationManager();
        possessionManager = this.EnsureComponent<PossessionManager>();
        slowMoManager = this.EnsureComponent<SlowMoManager>();

        if (!PlayerTutorial.runTutorial && !playerTeamsAlreadySelected)
        {
            cheatForcePlayerAssignment = true;
        }
        gameOver = false;
        InitializeTeams();
    }

    private void Start()
    {
        SceneStateManager.instance.UnPauseTime();

        meta = SceneStateManager.instance.gameObject;
        if (meta == null)
        {
            Debug.LogWarning("Meta object is null!!!!");
        }

        NotificationManager.CallOnMessage(Message.ScoreChanged, HandleGoalScored);
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

    // Used for pre-player object initialization, i.e. figuring out where to
    // spawn in new player objects during the "court pre-period". This period is
    // where no player objects exist, but all information about which objects
    // should exist, which teams they're on, and where they should spawn, is all
    // available through various static/room property maps of ints to ints.
    public TeamManager GetTeamAssignment(int playerNumber)
    {
        if (GameManager.playerTeamsAlreadySelected)
        {
            return Teams[playerTeamAssignments[playerNumber]];
        }
        else if (GameManager.cheatForcePlayerAssignment)
        {
            return Teams[playerNumber % Teams.Count];
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

    private void HandleGoalScored()
    {
        TeamManager topTeam = GetWinningTeam();
        if (topTeam != null && topTeam.Score >= Settings.WinningScore)
        {
            EndGame();
        } else
        {
            this.TimeDelayCall(ResetGameAfterGoal, Settings.PauseAfterGoalScore);
        }
    }

    private void ResetGameAfterGoal()
    {
        if (!gameOver)
        {
            NotificationManager.NotifyMessage(Message.ResetAfterGoal, this);
            NotificationManager.NotifyMessage(Message.StartCountdown, this);
        }

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

    // Networking info: frequently we need to work with/store player numbers
    // rather than player objects, due to network-synced data structures. This
    // utility function allows easy access to players given their player number,
    // but should potentially (and very easily could) be replaced with something
    // more efficient if it ends up being used in a tight loop.
    public Player GetPlayerFromNumber(int playerNumber) {
        return (from player in players
                where player.playerNumber == playerNumber
                select player).FirstOrDefault();
    }

    public List<Player> GetAllPlayers()
    {
        return players;
    }

    public List<Player> GetHumanPlayers()
    {
        return players.Where(player => player.playerNumber >= 0).ToList();
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
