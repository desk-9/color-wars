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

    void Start() {
        // We now have a new kind of "pre-period", where a scene loads and these
        // components all get initialized, but we're already connected to Photon
        // and in a room. When this happens, the Player Manager should re-spawn
        // player objects based on the information set in the (effectively
        // static) room properties. This works because the only information that
        // actually needs to carry over from the lobby to the court is which
        // player numbers are controlled by which actors, which teams their on,
        // and what their sprite number within that team is. None of this
        // requires consistency in actual player objects, just saving a few ints
        // across scenes.
        if (PhotonNetwork.InRoom) {
            SpawnExistingPlayers();
        }
    }

    void SpawnExistingPlayers() {
        LoadRoomProperties();
        // TODO Frame delay call! Currently the frame delay is necessary to
        // allow spawn point objects to register with the spawn point manager.
        // Unclear exactly how to handle, but should clean up.
        this.FrameDelayCall(() => {
                foreach (var (player, actor) in playerNumberToActorId) {
                    if (actor == PhotonNetwork.LocalPlayer.ActorNumber) {
                        SpawnMyPlayerObjects(player);
                    }
                }
            }, 3);
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

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        LoadRoomProperties();
    }

    // PR Comment: Could swear I'd noticed this didn't work and fixed it, but
    // guess it was in some stash somewhere I never committed. Either way, fixed
    // now.
    public override void OnPlayerLeftRoom(Photon.Realtime.Player gonePlayer)
    {
        LoadRoomProperties();
        ReleaseAllPlayers(gonePlayer.ActorNumber);
        if (PhotonNetwork.LocalPlayer.IsMasterClient) {
            // TODO(spruce): Pretty sure this is all correctly handled now by
            // Photon's automatic cleanup of objects instantiated by a player
            // when they leave the room (see Lifetime section in
            // https://doc.photonengine.com/en-us/pun/current/gameplay/instantiation)
            // DespawnLeft();
        }
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

    // TODO unused function, should probably clean up
    void DespawnLeft() {
        var spawnedPlayerNumbers = new HashSet<int>(
            from player in GameManager.Instance.players select player.playerNumber);
        var ownedPlayers = new HashSet<int>(
            from pair in playerNumberToActorId select pair.Key);
        var left = spawnedPlayerNumbers.Except(ownedPlayers);
        foreach (int playerNumber in left) {
            DespawnPlayerWithNumber(playerNumber);
        }
    }

    // TODO unused funciton, should clean up
    void DespawnPlayerWithNumber(int playerNumber) {
        var despawnPlayer = (from player in GameManager.Instance.players
                      where player.playerNumber == playerNumber
                      select player).FirstOrDefault();
        if (despawnPlayer != null) {
            PhotonNetwork.Destroy(despawnPlayer.GetComponent<PhotonView>());
        }
    }

    // Uses the spawn point manager to spawn locally owned players. Should ONLY
    // be called on playerNumbers actually controlled by this local network
    // actor.
    //
    // TODO add checks/errors
    void SpawnMyPlayerObjects(int playerNumber) {
        SpawnPointManager.Instance.SpawnPlayerWithNumber(playerNumber);
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
        SpawnMyPlayerObjects(nextPlayer);
        GameManager.NotificationManager.NotifyMessage(Message.PlayerAssignedPlayerNumber, this);
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
