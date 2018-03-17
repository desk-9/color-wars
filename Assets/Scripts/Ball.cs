using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class Ball : MonoBehaviour {

    Vector2 start_location;
    public BallCarrier owner { get; set; }
    public bool ownable {get; set;} = true;

    Rigidbody2D rb2d;

    public bool IsOwnable() {
        return owner == null && ownable;
    }

    void Start() {
        start_location = transform.position;
        rb2d = this.EnsureComponent<Rigidbody2D>();
    }

    public void ResetBall() {
        transform.position = start_location;
        ownable = true;
        rb2d.velocity = Vector2.zero;
    }
}
