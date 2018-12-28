using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UtilityExtensions;

public class NetworkManager : MonoBehaviourPunCallbacks, IConnectionCallbacks
{
    [SerializeField]
    private byte maxPlayersPerRoom = 4;

    // This client's version number. Users are separated by gameVersion.
    private string gameVersion = "1";

    private string gameRoomName = "set_this_to_something_else_locally";

    private void Awake()
    {
        // this ensures we can use PhotonNetwork.LoadLevel() on the master
        // client and all clients in the same room sync their level
        // automatically
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    private void Start()
    {
        Connect();
    }

    // Start the connection process.
    // - If already connected, we attempt joining a random room
    // - if not yet connected, Connect this application instance
    //   to Photon Cloud Network
    public void Connect()
    {
        // We have a new situation, loading Court *after* the lobby. This means
        // we're already connected, and already in a room. In this case, we
        // don't want to do anything at all
        if (PhotonNetwork.IsConnected && !PhotonNetwork.InRoom)
        {
            // Attempt to join the hardcoded room. Should never fail.
            //
            // TODO Why is this a frame delay call now? Can't remember
            this.FrameDelayCall(() => PhotonNetwork.JoinOrCreateRoom(gameRoomName, new RoomOptions { MaxPlayers = maxPlayersPerRoom }, TypedLobby.Default));
        }
        else
        {
            // First need to connect to Photon Online Server.
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Utility.Print("OnConnectedToMaster() was called by PUN");
        // The first we try to do is to join a potential existing room. If there
        // is, good, else, we'll be called back with OnJoinRandomFailed()
        PhotonNetwork.JoinOrCreateRoom(gameRoomName, new RoomOptions { MaxPlayers = maxPlayersPerRoom }, TypedLobby.Default);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Utility.Print("OnJoinRandomFailed() was called by PUN. No random room available, so we create one.");
        // Critical: we failed to join a random room, maybe none exists
        // or they are all full. So create a new room to join.
        PhotonNetwork.CreateRoom(gameRoomName, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
    }

    public override void OnJoinedRoom()
    {
        Utility.Print("OnJoinedRoom() called by PUN. This client is in a room.");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Utility.Print("OnDisconnected() was called by PUN with reason ", cause, LogLevel.Warning);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Utility.Print("Another peep joined the room yo!", LogLevel.Error);
    }
}
