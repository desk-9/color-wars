using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviour {
    
    Vector2 start_location;
    public BallCarrier owner { get; set; }
    public bool ownable {get; set;} = true;

    public bool IsOwnable() {
        return owner == null && ownable;
    }

    void Start() {
        start_location = transform.position;
    }

    public void ResetBall() {
        transform.position = start_location;
        ownable = true;
        this.EnsureComponent<Rigidbody2D>().velocity = Vector2.zero;
    }
}
