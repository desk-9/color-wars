using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// Handles spawning of networked players at correct spawn points
public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance = null;
    public SortedList<int, PlayerSpawnPoint> SpawnPoints = new SortedList<int, PlayerSpawnPoint>();

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(this);
        }
    }

    private PlayerSpawnPoint SpawnPointForPlayer(int playerNumber) {
        if (GameManager.playerTeamsAlreadySelected) {
            var team = GameManager.Instance.GetTeamAssignment(playerNumber);
            var spawnPointIndex = team.PlayerToSpawnPoint(playerNumber);
            return SpawnPoints[spawnPointIndex];
        } else {
            return SpawnPoints[playerNumber];
        }
    }

    public Player SpawnPlayerWithNumber(int playerNumber) {
        var spawnPoint = SpawnPointForPlayer(playerNumber);
        // Players that are instantiated from scratch start with a player number
        // of 0. While locally, we can set the player number in this function,
        // this isn't called in other clients. Instead we rely on the
        // instantiationData feature of Photon views. We send the player number
        // in the object array of data, and the Player Start() handles checking
        // for the existence of this data and setting its player number.
        //
        // This does mean currently only this class correctly encapsulates
        // spawning a new player, so it's really more of a PlayerSpawningManager
        // than just a SpawnPointManager...
        //
        // TODO Update class name
        var data = new object[] {playerNumber};
        var playerObject = PhotonNetwork.Instantiate("Player", spawnPoint.transform.position,
                                                     spawnPoint.transform.rotation, 0, data);
        var player = playerObject.GetComponent<Player>();
        player.playerNumber = playerNumber;
        player.initialPosition = player.transform.position;
        player.initialRotation = player.GetComponent<Rigidbody2D>().rotation;
        return player;
    }
}
