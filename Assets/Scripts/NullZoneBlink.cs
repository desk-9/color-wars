using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class NullZoneBlink : MonoBehaviour {

    public float flashOpacity = .6f;
    public float stayedFlashDuration = 0f;
    public float flashTransitionDuration = .1f;

    // Use this for initialization
    void Start () {
        GameModel.instance.nc.CallOnMessage(Message.NullChargePrevention, FlashNullZone);
    }

    void FlashNullZone() {
        if (this == null) {
            return;
        }
        var renderer = this.EnsureComponent<SpriteRenderer>();
        var startingAlpha = renderer.color.a;
        StartCoroutine(TransitionUtility.LerpAlpha(color => renderer.color = color,
                                                   renderer.color.a, flashOpacity, flashTransitionDuration, maybeTint : renderer.color));
        this.RealtimeDelayCall(() => FlashBackToNormal(startingAlpha), stayedFlashDuration+flashTransitionDuration);
    }

    void FlashBackToNormal(float alpha) {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) {
            return;
        }

        StartCoroutine(TransitionUtility.LerpAlpha(color => renderer.color = color, renderer.color.a, alpha, flashTransitionDuration, maybeTint : renderer.color));
    }
}
