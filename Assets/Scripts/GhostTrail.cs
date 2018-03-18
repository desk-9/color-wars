using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class GhostTrail : MonoBehaviour {

    public float ghostLifeLength = .2f;
    public GameObject ghostObject;

    new SpriteRenderer renderer;

    // Use this for initialization
    void Start () {
        renderer = this.EnsureComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update () {
        AddGhostObject();
    }

    void AddGhostObject() {
        var newGhostObject = GameObject.Instantiate(ghostObject, transform.position, transform.rotation);
        newGhostObject.transform.localScale = transform.localScale;
        newGhostObject.GetComponent<Ghost>().Initialize(renderer, ghostLifeLength);
    }
}
