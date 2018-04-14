using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class Blink : MonoBehaviour {

    new SpriteRenderer renderer;

    // Use this for initialization
    void Start () {
        renderer = this.EnsureComponent<SpriteRenderer>();
        StartCoroutine(BlinkRenderer());
    }

    IEnumerator BlinkRenderer() {
        while (true) {
            renderer.enabled = !renderer.enabled;
            yield return new WaitForSecondsRealtime(.5f);
        }
    }
}
