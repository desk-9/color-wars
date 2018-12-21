using System.Collections.Generic;
using UnityEngine;

public class TeamManager
{
    public static bool playerSpritesAlreadySet = false;
    private static Dictionary<int, Sprite> playerSpriteUsages = new Dictionary<int, Sprite>();
    public int teamNumber { get; set; }
    public NamedColor teamColor { get; set; }

    public int score { get; private set; }
    public List<Player> teamMembers = new List<Player>();
    public TeamResourceManager resources;
    private List<Sprite> memberSprites;
    private Dictionary<Player, Sprite> spriteUsage = new Dictionary<Player, Sprite>();
    private Stack<Sprite> unusedSprites;
    private List<float> playerXs = new List<float>() { -15, 15 };
    private List<float> playerYs = new List<float>() { -5, 27.7f };
    private Dictionary<int, int> teamNumberToX = new Dictionary<int, int>() {
        {0, 1},
        {1, 0},
    };

    public TeamManager(int teamNumber, NamedColor teamColor)
    {
        this.teamNumber = teamNumber;
        this.teamColor = teamColor;
        resources = new TeamResourceManager(this);
        memberSprites = new List<Sprite>() {
            resources.mainPlayerSprite, resources.altPlayerSprite
        };
        unusedSprites = new Stack<Sprite>(memberSprites);
    }


    public void ResetScore()
    {
        score = 0;
        GameManager.instance.notificationManager.NotifyMessage(Message.ScoreChanged, this);
        GameManager.instance.scoreDisplayer?.UpdateScores();
    }

    public void IncrementScore()
    {
        score += 1;
        GameManager.instance.notificationManager.NotifyMessage(Message.ScoreChanged, this);
        GameManager.instance.notificationManager.NotifyMessage(Message.GoalScored, this);
        GameManager.instance.scoreDisplayer?.UpdateScores();
    }

    private float CalculateRotation(int xIndex, int yIndex)
    {
        if (xIndex == 0 && yIndex == 0)
        {
            return 225;
        }
        else if (xIndex == 1 && yIndex == 0)
        {
            return 45;
        }
        else if (xIndex == 1 && yIndex == 1)
        {
            return 135;
        }
        else
        {
            return 315;
        }
    }

    public void AddTeamMember(Player newMember)
    {
        teamMembers.Add(newMember);
        Utility.Print(teamNumber);
        int yIndex = teamMembers.Count - 1;
        int xIndex = ((teamNumber - 1) + yIndex) % 2;
        newMember.initialPosition = new Vector2(
            playerXs[xIndex],
            playerYs[yIndex]);
        newMember.initialRotation = CalculateRotation(xIndex, yIndex);
        SpriteRenderer renderer = newMember.GetComponent<SpriteRenderer>();
        if (unusedSprites.Count == 0)
        {
            return;
        }
        Sprite sprite = unusedSprites.Peek();
        if (renderer != null && sprite != null)
        {
            renderer.color = teamColor;
            if (!playerSpritesAlreadySet)
            {
                renderer.sprite = sprite;
                spriteUsage[newMember] = sprite;
                playerSpriteUsages[newMember.playerNumber] = sprite;
                unusedSprites.Pop();
            }
            else
            {
                if (newMember.playerNumber >= 0)
                {
                    renderer.sprite = playerSpriteUsages[newMember.playerNumber];
                }
            }
        }
    }

    public void RemoveTeamMember(Player member)
    {
        if (teamMembers.Contains(member))
        {
            if (spriteUsage.ContainsKey(member))
            {
                unusedSprites.Push(spriteUsage[member]);
                spriteUsage.Remove(member);
            }
            teamMembers.Remove(member);
            if (!playerSpritesAlreadySet)
            {
                playerSpriteUsages.Remove(member.playerNumber);
            }
        }
    }

    public void MakeInvisibleAfterGoal()
    {
        foreach (Player teamMember in teamMembers)
        {
            teamMember.MakeInvisibleAfterGoal();
        }
    }

    public void ResetTeam()
    {
        foreach (Player teamMember in teamMembers)
        {
            teamMember.ResetPlayerPosition();
        }
    }

    public void BeginMovement()
    {
        foreach (Player teamMember in teamMembers)
        {
            teamMember.BeginPlayerMovement();
        }
    }
}
