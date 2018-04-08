using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class BallFillColor : MonoBehaviour {

    new SpriteRenderer renderer;

    // Use this for initialization
    void Start () {
        renderer = this.EnsureComponent<SpriteRenderer>();
    }

    public void EnableAndSetColor(Color to_) {
        renderer.enabled = true;
        renderer.color = to_;
    }

    public void DisableFill() {
        renderer.enabled = false;
    }
}
