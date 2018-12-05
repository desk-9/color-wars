using System.Collections.Generic;
using UnityEngine;

public class TeamManager
{
    public static bool playerSpritesAlreadySet = false;
    private static Dictionary<int, Sprite> playerSpriteUsages = new Dictionary<int, Sprite>();
    public int teamNumber { get; set; }
    public NamedColor color { get; set; }

    public int score { get; private set; }
    public List<Player> members = new List<Player>();
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
    private Stack<float> unusedYs;

    public TeamManager(int teamNumber, NamedColor color)
    {
        this.teamNumber = teamNumber;
        this.color = color;
        resources = new TeamResourceManager(this);
        memberSprites = new List<Sprite>() {
            resources.mainPlayerSprite, resources.altPlayerSprite
        };
        unusedSprites = new Stack<Sprite>(memberSprites);
        unusedYs = new Stack<float>(playerYs);
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

    private float CalculateRotation(Vector2 position)
    {
        int xIndex = playerXs.IndexOf(position.x);
        int yIndex = playerYs.IndexOf(position.y);
        if (xIndex == 0 && yIndex == 0)
        {
            return 45;
        }
        else if (xIndex == 1 && yIndex == 0)
        {
            return 135;
        }
        else if (xIndex == 1 && yIndex == 1)
        {
            return 225;
        }
        else
        {
            return 315;
        }
    }

    public void AddTeamMember(Player newMember)
    {
        members.Add(newMember);
        Utility.Print(teamNumber);
        if (unusedYs.Count > 0)
        {
            newMember.initialPosition = new Vector2(
                                                    playerXs[teamNumberToX[teamNumber - 1]],
                                                    unusedYs.Pop());
            newMember.initialRotation = CalculateRotation(newMember.initialPosition);
        }
        SpriteRenderer renderer = newMember.GetComponent<SpriteRenderer>();
        if (unusedSprites.Count == 0)
        {
            return;
        }
        Sprite sprite = unusedSprites.Peek();
        if (renderer != null && sprite != null)
        {
            renderer.color = color;
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
        if (members.Contains(member))
        {
            unusedYs.Push(member.initialPosition.y);
            if (spriteUsage.ContainsKey(member))
            {
                unusedSprites.Push(spriteUsage[member]);
                spriteUsage.Remove(member);
            }
            members.Remove(member);
            if (!playerSpritesAlreadySet)
            {
                playerSpriteUsages.Remove(member.playerNumber);
            }
        }
    }

    public void MakeInvisibleAfterGoal()
    {
        foreach (Player teamMember in members)
        {
            teamMember.MakeInvisibleAfterGoal();
        }
    }

    public void ResetTeam()
    {
        foreach (Player teamMember in members)
        {
            teamMember.ResetPlayerPosition();
        }
    }

    public void BeginMovement()
    {
        foreach (Player teamMember in members)
        {
            teamMember.BeginPlayerMovement();
        }
    }
}
