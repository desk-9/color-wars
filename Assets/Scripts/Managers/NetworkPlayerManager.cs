using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UtilityExtensions;
using Hashtable = ExitGames.Client.Photon.Hashtable;

/// <summary>
/// This class handles storing, setting, and communicating the ownership of
/// Player objects by Photon network Players.
/// </summary>
public class NetworkPlayerManager : MonoBehaviourPunCallbacks, IConnectionCallbacks
{
    public static string FREE_PLAYERS_PROPERTY_KEY = "__FreePlayersPropertiesKey__";
    public static string PLAYER_OWNERS_PROPERTIES_KEY = "__PlayerOwnersPropertiesKey__";
    public static NetworkPlayerManager Instance { get; set; }

    // This is currently just set as a literal, but will almost certainly
    // change, especially as we add a lobby system.
    List<int> freePlayers = new List<int> { 1, 2, 3, 4 };

    /// <summary>
    /// Maps player number to photon actorID
    /// </summary>
    Dictionary<int, int> playerNumberToActorId = new Dictionary<int, int>();

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

    /// <summary>
    /// Ensures the free players is populated by the first player to enter
    /// the room
    /// </summary>
    void EnsureRoomPropertiesExist()
    {
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(FREE_PLAYERS_PROPERTY_KEY))
        {
            var outData = new Hashtable();
            outData.Add(FREE_PLAYERS_PROPERTY_KEY, freePlayers.ToArray());
            outData.Add(PLAYER_OWNERS_PROPERTIES_KEY, new Dictionary<int, int>());
            PhotonNetwork.CurrentRoom.SetCustomProperties(outData);
        }
    }

    /// <summary>
    /// Load the networked property data to the local variables.
    /// </summary>
    void LoadRoomProperties()
    {
        freePlayers = new List<int>(
            PhotonNetwork.CurrentRoom.CustomProperties[FREE_PLAYERS_PROPERTY_KEY] as int[]);
        playerNumberToActorId = PhotonNetwork.CurrentRoom.CustomProperties[PLAYER_OWNERS_PROPERTIES_KEY] as Dictionary<int, int>;
    }

    /// <summary>
    /// Push the values of the local variables to the networked properties
    /// </summary>
    void SetRoomPropertiesFromLocalData()
    {
        Hashtable outData = new Hashtable {
            {FREE_PLAYERS_PROPERTY_KEY, freePlayers.ToArray()},
            {PLAYER_OWNERS_PROPERTIES_KEY, playerNumberToActorId}
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(outData);
    }

    /// <summary>
    /// Takes possession of the next available player object for the local
    /// network player.
    /// </summary>
    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
        {
            EnsureRoomPropertiesExist();
        }
        OwnNextPlayer(PhotonNetwork.LocalPlayer.ActorNumber);
    }

    // TODO dkonik: I don't think this will work. I think this is *after* a player
    // left the room. I think what will have to happen is the other players will have
    // to listen for when a user leaves the room and someone (probably master) will ahve to 
    // do this cleanup
    public override void OnLeftRoom()
    {
        //ReleaseAllPlayers(PhotonNetwork.LocalPlayer.ActorNumber);
    }

    /// <summary>
    /// Returns whether or not the specified player number is the locally controlled
    /// </summary>
    /// <param name="playerNumber"></param>
    /// <returns></returns>
    public bool LocalOwnsPlayer(int playerNumber)
    {
        if (playerNumberToActorId.ContainsKey(playerNumber))
        {
            return playerNumberToActorId[playerNumber] == PhotonNetwork.LocalPlayer.ActorNumber;
        }
        return false;
    }

    /// <summary>
    /// Takes control of some free player object for the given actor ID
    /// (networked player ID), and sync state change to other networked
    /// players and player objects.
    /// </summary>
    /// <param name="actorId"></param>
    void OwnNextPlayer(int actorId)
    {
        LoadRoomProperties();
        int nextPlayer = freePlayers.Last();
        freePlayers.Remove(nextPlayer);
        playerNumberToActorId.Add(nextPlayer, actorId);
        SetRoomPropertiesFromLocalData();
        foreach (var player in GameManager.instance.players)
        {
            player.HandlePlayerNumberAssigned();
        }
        GameManager.instance.NotificationManager.NotifyMessage(Message.PlayerAssignedPlayerNumber, this);
    }

    /// <summary>
    /// Releases all player objects owned by the given network ID.
    /// </summary>
    /// <param name="actorId"></param>
    void ReleaseAllPlayers(int actorId)
    {
        LoadRoomProperties();
        var nowFree = playerNumberToActorId.Where(pair => pair.Value == actorId)
            .Select(pair => pair.Key).ToList();
        foreach (int playerId in nowFree)
        {
            ReleasePlayer_Internal(playerId);
        }
        SetRoomPropertiesFromLocalData();

    }

    /// <summary>
    /// Internal helper used by ReleasePlayer
    /// </summary>
    /// <param name="playerId"></param>
    void ReleasePlayer_Internal(int playerId)
    {
        freePlayers.Add(playerId);
        playerNumberToActorId.Remove(playerId);
    }
}
