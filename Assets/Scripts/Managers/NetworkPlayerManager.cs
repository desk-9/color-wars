using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UtilityExtensions;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class NetworkPlayerManager : MonoBehaviourPunCallbacks, IConnectionCallbacks
{
    // This class handles storing, setting, and communicating the ownership of
    // Player objects by Photon network Players.
    public static string FREE_PLAYERS_PROPERTY_KEY = "__FreePlayersPropertiesKey__";
    public static string PLAYER_OWNERS_PROPERTIES_KEY = "__PlayerOwnersPropertiesKey__";
    public static NetworkPlayerManager instance;
    // This is currently just set as a literal, but will almost certainly
    // change, especially as we add a lobby system.
    List<int> freePlayers = new List<int> { 1, 2, 3, 4 };
    Dictionary<int, int> playerOwners = new Dictionary<int, int>();
    PhotonView photonView;

    void Awake()
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

    // Start is called before the first frame update
    void Start()
    {
        Utility.Print("Mapping manager", LogLevel.Error);
        photonView = GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FirstLoad()
    {
        // Ensures the free players is populated by the first player to enter
        // the room
        Utility.Print("Did First Load for NetworkPlayerManager in room", LogLevel.Warning);
        if (!PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(FREE_PLAYERS_PROPERTY_KEY))
        {
            var outData = new Hashtable();
            outData.Add(FREE_PLAYERS_PROPERTY_KEY, freePlayers.ToArray());
            outData.Add(PLAYER_OWNERS_PROPERTIES_KEY, new Dictionary<int, int>());
            PhotonNetwork.CurrentRoom.SetCustomProperties(outData);
        }
    }

    void LoadData()
    {
        // Load the networked property data to the local variables.
        freePlayers = new List<int>(
            PhotonNetwork.CurrentRoom.CustomProperties[FREE_PLAYERS_PROPERTY_KEY] as int[]);
        playerOwners = PhotonNetwork.CurrentRoom.CustomProperties[PLAYER_OWNERS_PROPERTIES_KEY] as Dictionary<int, int>;
    }

    void SetData()
    {
        // Push the values of the local variables to the networked properties
        Hashtable outData = new Hashtable {
            {FREE_PLAYERS_PROPERTY_KEY, freePlayers.ToArray()},
            {PLAYER_OWNERS_PROPERTIES_KEY, playerOwners}
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(outData);
    }

    public override void OnJoinedRoom()
    {
        // Takes possession of the next available player object for the local
        // network player.
        if (PhotonNetwork.CurrentRoom.PlayerCount == 1) {
            FirstLoad();
        }
        OwnNextPlayer(PhotonNetwork.LocalPlayer.ActorNumber);
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player leaver)
    {
        ReleaseAllPlayers(leaver.ActorNumber);
    }

    public bool LocalOwnsPlayer(int playerNumber)
    {
        // Whether the local network player owns a certain player object number
        if (playerOwners.ContainsKey(playerNumber))
        {
            return playerOwners[playerNumber] == PhotonNetwork.LocalPlayer.ActorNumber;
        }
        return false;
    }

    void OwnNextPlayer(int actorId)
    {
        // Takes control of some free player object for th given actor ID
        // (networked player ID), and sync state change to other networked
        // players and player objects.
        LoadData();
        int nextPlayer = freePlayers.Last();
        freePlayers.Remove(freePlayers.Count);
        playerOwners.Add(nextPlayer, actorId);
        SetData();
        foreach (var player in GameManager.instance.players)
        {
            player.HandleOwnership();
        }

    }

    void ReleaseAllPlayers(int actorId)
    {
        // Releases all player objects owned by the given network ID.
        LoadData();
        var nowFree = playerOwners.Where(pair => pair.Value == actorId)
            .Select(pair => pair.Key).ToList();
        foreach (int playerId in nowFree)
        {
            ReleasePlayer_Internal(playerId);
        }
        SetData();

    }

    void ReleasePlayer(int playerId)
    {
        // Releases a single player object from whatever actor owns it.
        LoadData();
        ReleasePlayer_Internal(playerId);
        SetData();
    }

    void ReleasePlayer_Internal(int playerId)
    {
        // Internal helper used by ReleasePlayer
        freePlayers.Add(playerId);
        playerOwners.Remove(playerId);
    }
}
