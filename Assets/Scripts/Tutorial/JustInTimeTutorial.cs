using UnityEngine;
using System.Linq;

public class JustInTimeTutorial : MonoBehaviour
{
    public static JustInTimeTutorial instance;
    public static bool alreadySeen = false;
    private int scoreThreshold = 0;
    private GameObject canvasPrefab;

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
        canvasPrefab = Resources.Load<GameObject>("ToolTipCanvas");
        GameManager.instance.notificationManager.CallOnMessageWithPlayerObject(
            Message.BallPossessedWhileNeutral, PassToTeammate);
        GameManager.instance.notificationManager.CallOnMessageWithPlayerObject(
            Message.BallPossessedWhileCharged, ShootAtGoal);
        GameManager.instance.notificationManager.CallOnMessage(
            Message.GoalScored,
            () =>
            {
                if (!alreadySeen && GameManager.instance.teams.All(
                        team => team.score > scoreThreshold))
                {
                    alreadySeen = true;
                }
            });

        GameManager.instance.notificationManager.CallOnStateEnd(State.Posession, Unpossessed);

        GameManager.instance.notificationManager.CallOnMessage(
            Message.PlayerReleasedBack,
            () =>
            {
                scoreThreshold = GameManager.instance.teams.Max(team => team.score);
                alreadySeen = false;
            });
        // On possession loss: no text
        // On possession with neutral: pass to teammate
        // On possession with charged: shoot at goal
    }

    private void Unpossessed(Player player)
    {
        ToolTipPlacement tooltipCanvas = player.GetComponentInChildren<ToolTipPlacement>();
        if (tooltipCanvas != null)
        {
            tooltipCanvas.SetText("");
        }
    }

    private ToolTipPlacement CheckMakeCanvas(Player player)
    {
        ToolTipPlacement tooltipCanvas = player?.GetComponentInChildren<ToolTipPlacement>();
        if (tooltipCanvas == null && player != null)
        {
            tooltipCanvas = Instantiate(canvasPrefab, player.transform).GetComponent<ToolTipPlacement>();
        }
        return tooltipCanvas;
    }

    private void PassToTeammate(object sender)
    {
        Player player = sender as Player;
        ToolTipPlacement tooltipCanvas = CheckMakeCanvas(player);
        if (!alreadySeen && player != null && player.team != null
            && player.team.score <= scoreThreshold)
        {
            tooltipCanvas?.SetText("<AButton> Pass to your teammate");
        }
        else
        {
            tooltipCanvas?.SetText("");
        }
    }

    private void ShootAtGoal(object sender)
    {
        Player player = sender as Player;
        ToolTipPlacement tooltipCanvas = CheckMakeCanvas(player);
        if (!alreadySeen && player != null && player.team != null
            && player.team.score <= scoreThreshold)
        {
            tooltipCanvas?.SetText("<AButton> Shoot at the goal");
        }
        else
        {
            tooltipCanvas?.SetText("");
        }
    }
}
