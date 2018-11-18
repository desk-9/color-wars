using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class GhostTrail : MonoBehaviour {

    public float ghostLifeLength = .2f;
    public GameObject ghostObject;
    public float timeBetweenGhosts;
    public float startingAlpha;

    new SpriteRenderer renderer;
    Coroutine ghostCoroutine;

    // Use this for initialization
    void Start () {
        if (this == null) {
            return;
        }
        renderer = this.EnsureComponent<SpriteRenderer>();
        var nc = GameModel.instance.notificationCenter;
        nc.CallOnMessage(Message.BallIsPossessed, StartGhost);
        nc.CallOnMessage(Message.BallIsUnpossessed, StopGhost);
    }

    void StartGhost() {
        if (this == null) {
            return;
        }
        if (ghostCoroutine == null) {
            ghostCoroutine = StartCoroutine(AddGhostObject());
        }
    }

    void StopGhost() {
        if (this == null) {
            return;
        }
        if (ghostCoroutine != null) {
            StopCoroutine(ghostCoroutine);
            ghostCoroutine = null;
        }
    }

    IEnumerator AddGhostObject() {
        while (true) {
            var newGhostObject = GameObject.Instantiate(ghostObject, transform.position, transform.rotation);
            newGhostObject.transform.localScale = transform.localScale;
            newGhostObject.GetComponent<Ghost>().Initialize(renderer, ghostLifeLength, startingAlpha);
            yield return null;
        }
    }
}
