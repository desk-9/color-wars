using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ball : MonoBehaviour {

    // A player is allowed to "possess" the ball
    GameObject currentOwner = null;
    
    public void SetOwner(GameObject newOwner) {
        currentOwner = newOwner;
    }

    public void ClearOwner() {
        currentOwner = null;
    }
    
}
