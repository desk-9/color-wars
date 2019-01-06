using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This component does nothing except effectively register a transform (position
// and rotation) with the spawn point manager
public class PlayerSpawnPoint : MonoBehaviour
{
    public int SpawnPointNumber = 0;
    void Start()
    {
        SpawnPointManager.Instance.SpawnPoints.Add(SpawnPointNumber, this);
    }
}
