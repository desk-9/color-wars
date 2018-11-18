using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class TeamManager {
    public static bool playerSpritesAlreadySet = false;
    static Dictionary<int, Sprite> playerSpriteUsages = new Dictionary<int, Sprite>();
    public int teamNumber { get; set; }
    public NamedColor teamColor { get; set; }

    public int score {get; private set;}
    public List<Player> teamMembers = new List<Player>();
    public TeamResourceManager resources;

    List<Sprite> memberSprites;
    Dictionary<Player, Sprite> spriteUsage = new Dictionary<Player, Sprite>();
    Stack<Sprite> unusedSprites;

    List<float> playerXs = new List<float>() {-15, 15};
    List<float> playerYs = new List<float>() {-5, 27.7f};

    Dictionary<int, int> teamNumberToX = new Dictionary<int, int>() {
        {0, 1},
        {1, 0},
    };

    Stack<float> unusedYs;

    public TeamManager(int teamNumber, NamedColor teamColor) {
        this.teamNumber = teamNumber;
        this.teamColor = teamColor;
        resources = new TeamResourceManager(this);
        memberSprites = new List<Sprite>() {
            resources.mainPlayerSprite, resources.altPlayerSprite
        };
        unusedSprites = new Stack<Sprite>(memberSprites);
        unusedYs = new Stack<float>(playerYs);
    }


    public void ResetScore() {
        score = 0;
        GameModel.instance.notificationCenter.NotifyMessage(Message.ScoreChanged, this);
        GameModel.instance.scoreDisplayer?.UpdateScores();
    }

    public void IncrementScore() {
        score += 1;
        GameModel.instance.notificationCenter.NotifyMessage(Message.ScoreChanged, this);
        GameModel.instance.notificationCenter.NotifyMessage(Message.GoalScored, this);
        GameModel.instance.scoreDisplayer?.UpdateScores();
    }

    float CalculateRotation(Vector2 position) {
        int xIndex = playerXs.IndexOf(position.x);
        int yIndex = playerYs.IndexOf(position.y);
        if (xIndex == 0 && yIndex == 0) {
            return 45;
        } else if (xIndex == 1 && yIndex == 0) {
            return 135;
        } else if (xIndex == 1 && yIndex == 1) {
            return 225;
        } else {
            return 315;
        }
    }

    public void AddTeamMember(Player newMember) {
        teamMembers.Add(newMember);
        Utility.Print(teamNumber);
        if (unusedYs.Count > 0) {
            newMember.initialPosition = new Vector2(
                                                    playerXs[teamNumberToX[teamNumber-1]],
                                                    unusedYs.Pop());
            newMember.initialRotation = CalculateRotation(newMember.initialPosition);
        }
        var renderer = newMember.GetComponent<SpriteRenderer>();
        if (unusedSprites.Count == 0) {
            return;
        }
        var sprite = unusedSprites.Peek();
        if (renderer != null && sprite != null) {
            renderer.color = teamColor;
            if (!playerSpritesAlreadySet) {
                renderer.sprite = sprite;
                spriteUsage[newMember] = sprite;
                playerSpriteUsages[newMember.playerNumber] = sprite;
                unusedSprites.Pop();
            } else {
                if (newMember.playerNumber >= 0) {
                    renderer.sprite = playerSpriteUsages[newMember.playerNumber];
                }
            }
        }
    }

    public void RemoveTeamMember(Player member) {
        if (teamMembers.Contains(member)) {
            unusedYs.Push(member.initialPosition.y);
            if (spriteUsage.ContainsKey(member)) {
                unusedSprites.Push(spriteUsage[member]);
                spriteUsage.Remove(member);
            }
            teamMembers.Remove(member);
            if (!playerSpritesAlreadySet) {
                playerSpriteUsages.Remove(member.playerNumber);
            }
        }
    }

    public void MakeInvisibleAfterGoal() {
        foreach (var teamMember in teamMembers) {
            teamMember.MakeInvisibleAfterGoal();
        }
    }

    public void ResetTeam() {
        foreach (var teamMember in teamMembers) {
            teamMember.ResetPlayerPosition();
        }
    }

    public void BeginMovement() {
        foreach (var teamMember in teamMembers) {
            teamMember.BeginPlayerMovement();
        }
    }
}
