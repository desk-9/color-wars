using UnityEngine;
using Photon.Pun;
using Photon.Realtime;


public class NetworkManager : MonoBehaviourPunCallbacks, IConnectionCallbacks
{
    [SerializeField]
    private byte maxPlayersPerRoom = 4;

    // This client's version number. Users are separated by gameVersion.
    private string gameVersion = "1";

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
        if (PhotonNetwork.IsConnected)
        {
            // Must attempt to join a Random Room. If it fails, we'll get
            // notified in OnJoinRandomFailed() and we'll create one.
            PhotonNetwork.JoinRandomRoom();
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
        Utility.Log("OnConnectedToMaster() was called by PUN");
        // The first we try to do is to join a potential existing room. If there
        // is, good, else, we'll be called back with OnJoinRandomFailed()
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Utility.Log("OnJoinRandomFailed() was called by PUN."
                      "No random room available, so we create one.");
        // Critical: we failed to join a random room, maybe none exists
        // or they are all full. So create a new room to join.
        PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
    }

    public override void OnJoinedRoom()
    {
        Utility.Log("OnJoinedRoom() called by PUN. This client is in a room.");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Utility.Log("OnDisconnected() was called by PUN with reason ", cause, LogLevel.Warning);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        Utility.Log("Another peep joined the room yo!", LogLevel.Error);
    }
}
