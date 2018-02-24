using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamManager : MonoBehaviour {
	public int teamNumber = 0;
	
	void Start () {
        GameModel.instance.RegisterTeam(this);
	}
    
}
