using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UtilityExtensions;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// This class handles syncing state between TeamManagers across the network.
// It's only necessary in the lobby, where team state can actually change.
//
// TODO this follows a very similar usage patter of room properties as the
// NetworkPlayerManager, should generalize the pattern and refactor
public class NetworkTeamManager : MonoBehaviourPunCallbacks, IConnectionCallbacks, IInRoomCallbacks
{
    public static string TEAMS_PROPERTY_KEY = "__TeamsPropertyKey__";
    public static NetworkTeamManager Instance { get; set; }

    // In the Court scene, there's no reason for this class to do anything (and
    // some errors occur if it's still running).
    //
    // TODO disable this and actually fix those errors. Minor cleanup.
    bool doNothing = false;
    // Map team number to a map of player numbers to sprite numbers.
    Dictionary<int, Dictionary<int, int>> teamState = new Dictionary<int, Dictionary<int, int>>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    void Start() {
        if (GameManager.playerTeamsAlreadySelected) {
            // If the network team manager is initialized in a scene where teams
            // have already been selected, there's no reason to continue syncing
            // team information. It's static for the rest of the game.
            doNothing = true;
        }
        // TODO if we wanted state to remain synced in Court, after scene load,
        // we would have to do a LoadRoomProperties in start rather than room
        // join (since the room is already joined in court now). We don't
        // actually want this, just here as an example of usage of this room
        // properties pattern
        //
        // if (PhotonNetwork.InRoom) {
        //     LoadRoomProperties();
        //     this.FrameDelayCall(() => Pull(), 2);
        // }
    }

    /// <summary> Ensures team data is populated by the first player to ender
    /// the room
    /// </summary>
    void EnsureRoomPropertiesExist()
    {
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(TEAMS_PROPERTY_KEY))
        {
            var outData = new Hashtable();
            outData.Add(TEAMS_PROPERTY_KEY, new Dictionary<int, Dictionary<int, int>>());
            PhotonNetwork.CurrentRoom.SetCustomProperties(outData);
        }
    }

    /// <summary>
    /// Load the networked property data to the local variables.
    /// </summary>
    void LoadRoomProperties()
    {
        teamState = PhotonNetwork.CurrentRoom.CustomProperties[TEAMS_PROPERTY_KEY] as Dictionary<int, Dictionary<int, int>>;
    }

    /// <summary>
    /// Push the values of the local variables to the networked properties
    /// </summary>
    void SetRoomPropertiesFromLocalData()
    {
        Hashtable outData = new Hashtable {
            {TEAMS_PROPERTY_KEY, teamState}
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(outData);
    }

    // Push data from the local state to the network
    public void Push() {
        if (doNothing) {return;}
        ReadFromTeamManagers();
        SetRoomPropertiesFromLocalData();
    }

    // Pull data from the network to the local state
    public void Pull() {
        if (doNothing) {return;}
        LoadRoomProperties();
        PushToTeamManagers();
    }

    // TODO think this is totally unused
    public int SpriteNumber(int teamNumber, int playerNumber) {
        Utility.Print("In sprite number FIRST", teamNumber, playerNumber, LogLevel.Error);
        foreach (var (t, d) in teamState) {
            foreach (var (p, s) in d) {
                Utility.Print("In sprite number", t, p, s, LogLevel.Error);
            }
        }
        return teamState[teamNumber - 1][playerNumber];
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            EnsureRoomPropertiesExist();
        }
        // TODO frame delay call: can't remember/maybe never knew why this is
        // necessary, only that things broke without it. Should investigate and
        // remove.
        this.FrameDelayCall(() =>{ Pull(); });
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) {
        Debug.LogError("Room properties updated");
        // TODO frame delay call: can't remember/maybe never knew why this is
        // necessary, only that things broke without it. Should investigate and
        // remove.
        this.FrameDelayCall(() =>{ Pull(); });
    }

    public void ReadFromTeamManagers() {
        foreach (var team in GameManager.Instance.Teams) {
            teamState[team.TeamNumber] = team.ConvertForNetwork();
        }
    }

    public void PushToTeamManagers() {
        var stateCopy = new Dictionary<int, Dictionary<int, int>>(teamState);
        foreach (var (teamNumber, teamData) in stateCopy) {
            var team = (from t in GameManager.Instance.Teams
                            where t.TeamNumber == teamNumber
                            select t).First();
            foreach (var (playerNumber, spriteNumber) in teamData) {
                var player = GameManager.Instance.GetPlayerFromNumber(playerNumber);
                player.SetTeam(team, spriteNumber);
            }
        }
    }
}
