using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UtilityExtensions;

public class TeamManager
{
    public static bool playerSpritesAlreadySet = false;
    private static Dictionary<int, Sprite> playerSpriteUsages = new Dictionary<int, Sprite>();
    // We have a new "concept", the player sprite number, which for our purposes
    // is the same idea as it's "roster" or "jersey" number on the team
    private static Dictionary<int, int> playerSpriteNumbers = new Dictionary<int, int>();
    public int TeamNumber { get; set; }
    public NamedColor TeamColor { get; set; }

    public int Score { get; private set; }
    public List<Player> teamMembers = new List<Player>();
    public TeamResourceManager resources;
    private List<Sprite> memberSprites;
    private Dictionary<Player, Sprite> spriteUsage = new Dictionary<Player, Sprite>();
    private List<Sprite> unusedSprites;

    public TeamManager(int teamNumber, NamedColor teamColor)
    {
        this.TeamNumber = teamNumber;
        this.TeamColor = teamColor;
        resources = new TeamResourceManager(this);
        memberSprites = new List<Sprite>() {
            resources.mainPlayerSprite, resources.altPlayerSprite
        };
        unusedSprites = new List<Sprite>(memberSprites);

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


    // TODO some of these functions might be unused now
    public int PlayerToSpawnPoint(Player player) {
        if (!teamMembers.Contains(player)) {
            return -1;
        }
        return 1 + (TeamNumber - 1) + (PlayerNumberInTeam(player) * 2);
    }

    public int PlayerToSpawnPoint(int playerNumber) {
        int spriteNumber = playerSpriteNumbers[playerNumber];
        return 1 + (TeamNumber - 1) + (spriteNumber * 2);
    }

    // Player number in its team based on sprite, 0-indexed
    public int PlayerNumberInTeam(Player player) {
        if (!teamMembers.Contains(player)) {
            return -1;
        }
        return SpriteNumber(spriteUsage[player]);
    }

    int SpriteNumber(Sprite sprite) {
        return (sprite == resources.mainPlayerSprite) ? 1 : 0;
    }

    public int PlayerNumberToSpriteNumber(int playerNumber) {
        var player = GameManager.Instance.GetPlayerFromNumber(playerNumber);
        if (!spriteUsage.ContainsKey(player)) {
            return -1;
        }
        return memberSprites.IndexOf(spriteUsage[player]);
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

    // It's now possible for the network to force a certain sprite number to be
    // chosen even in the team selection stage
    public void AddTeamMember(Player newMember, int forceSpriteNumber = -1)
    {
        teamMembers.Add(newMember);
        Utility.Print("Adding team member!", TeamNumber);
        SpriteRenderer renderer = newMember.GetComponent<SpriteRenderer>();
        if (unusedSprites.Count == 0)
        {
            return;
        }

        Sprite sprite = unusedSprites.First();
        if (forceSpriteNumber >= 0) {
            sprite = memberSprites[forceSpriteNumber];

        }
        if (renderer != null && sprite != null)
        {
            renderer.color = TeamColor;
            if (!playerSpritesAlreadySet)
            {
                renderer.sprite = sprite;
                spriteUsage[newMember] = sprite;
                playerSpriteUsages[newMember.playerNumber] = sprite;
                playerSpriteNumbers[newMember.playerNumber] = SpriteNumber(sprite);
                unusedSprites.Remove(sprite);
            }
            else
            {
                if (newMember.playerNumber >= 0)
                {
                    renderer.sprite = playerSpriteUsages[newMember.playerNumber];
                    spriteUsage[newMember] = renderer.sprite;
                }
            }
        }
        if (forceSpriteNumber < 0 && !playerSpritesAlreadySet) {
            // Send changes in teams out to the network. Send nothing if this
            // change was initiated by the network in the first place (i.e.
            // force sprite number >= 0)
            NetworkTeamManager.Instance.Push();
        }
        GameManager.NotificationManager.NotifyMessage(Message.TeamsChanged, null);
    }

    public void RemoveTeamMember(Player member)
    {
        if (teamMembers.Contains(member))
        {
            if (spriteUsage.ContainsKey(member))
            {
                unusedSprites.Add(spriteUsage[member]);
                spriteUsage.Remove(member);
            }
            teamMembers.Remove(member);
            if (!playerSpritesAlreadySet)
            {
                playerSpriteUsages.Remove(member.playerNumber);
                playerSpriteNumbers.Remove(member.playerNumber);
            }
        }
        if (!playerSpritesAlreadySet) {
            // Send changes in teams out to the network
            NetworkTeamManager.Instance.Push();
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

    // Convert the team manager into a serialized data structure of mapping each
    // player number to a sprite/roster number.
    //
    // TODO use actual photon serialization interfaces
    public Dictionary<int, int> ConvertForNetwork() {
        var data = new Dictionary<int, int>();
        foreach (var player in teamMembers) {
            data[player.playerNumber] = PlayerNumberToSpriteNumber(player.playerNumber);
        }
        return data;
    }
}
