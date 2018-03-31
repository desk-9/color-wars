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

    public TeamManager(int teamNumber, NamedColor teamColor) {
        this.teamNumber = teamNumber;
        this.teamColor = teamColor;
        resources = new TeamResourceManager(this);
        memberSprites = new List<Sprite>() {
            resources.mainPlayerSprite, resources.altPlayerSprite
        };
        unusedSprites = new Stack<Sprite>(memberSprites);
    }


    public void ResetScore() {
        score = 0;
        GameModel.instance.scoreDisplayer?.UpdateScores();
    }

    public void IncrementScore() {
        score += 1;
        GameModel.instance.scoreDisplayer?.UpdateScores();
    }

    public void AddTeamMember(Player newMember) {
        teamMembers.Add(newMember);
        var renderer = newMember.GetComponent<SpriteRenderer>();
        var sprite = unusedSprites.Peek();
        if (renderer != null && sprite != null) {
            renderer.color = teamColor;

            if (!playerSpritesAlreadySet) {
                renderer.sprite = sprite;
                spriteUsage[newMember] = sprite;
                playerSpriteUsages[newMember.playerNumber] = sprite;
                unusedSprites.Pop();
            } else {
                renderer.sprite = playerSpriteUsages[newMember.playerNumber];
            }
        }
    }

    public void RemoveTeamMember(Player member) {
        if (teamMembers.Contains(member)) {
            unusedSprites.Push(spriteUsage[member]);
            spriteUsage.Remove(member);
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
