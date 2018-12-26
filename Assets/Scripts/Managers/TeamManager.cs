using System.Collections.Generic;
using UnityEngine;

public class TeamManager
{
    public static bool playerSpritesAlreadySet = false;
    private static Dictionary<int, Sprite> playerSpriteUsages = new Dictionary<int, Sprite>();
    public int TeamNumber { get; set; }
    public NamedColor TeamColor { get; set; }

    public int Score { get; private set; }
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
    private Stack<float> unusedYs;

    public TeamManager(int teamNumber, NamedColor teamColor)
    {
        this.TeamNumber = teamNumber;
        this.TeamColor = teamColor;
        resources = new TeamResourceManager(this);
        memberSprites = new List<Sprite>() {
            resources.mainPlayerSprite, resources.altPlayerSprite
        };
        unusedSprites = new Stack<Sprite>(memberSprites);
        unusedYs = new Stack<float>(playerYs);

        GameManager.NotificationManager.CallOnMessage(Message.GoalScored, HandleGoalScored, true);
        GameManager.NotificationManager.CallOnMessage(Message.ResetAfterGoal, ResetTeam);
        GameManager.NotificationManager.CallOnMessage(Message.CountdownFinished, HandleRoundStartCountdownFinished);
    }

    private void HandleGoalScored()
    {
        if (GameManager.PossessionManager.CurrentTeam == this)
        {
            // We scored a goal
            IncrementScore();
        } else
        {
            // Other team scored a goal
            MakeInvisibleAfterGoal();
        }
    }

    private void HandleRoundStartCountdownFinished()
    {
        ResetTeam();
        BeginMovement();
    }

    private void IncrementScore()
    {
        Score += 1;
        GameManager.NotificationManager.NotifyMessage(Message.ScoreChanged, this);
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
        teamMembers.Add(newMember);
        Utility.Print(TeamNumber);
        if (unusedYs.Count > 0)
        {
            newMember.initialPosition = new Vector2(
                                                    playerXs[teamNumberToX[TeamNumber - 1]],
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
            renderer.color = TeamColor;
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
            unusedYs.Push(member.initialPosition.y);
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

    private void MakeInvisibleAfterGoal()
    {
        foreach (Player teamMember in teamMembers)
        {
            teamMember.MakeInvisibleAfterGoal();
        }
    }

    private void ResetTeam()
    {
        foreach (Player teamMember in teamMembers)
        {
            teamMember.ResetPlayerAfterGoal();
        }
    }

    private void BeginMovement()
    {
        foreach (Player teamMember in teamMembers)
        {
            teamMember.BeginPlayerMovement();
        }
    }
}
