using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class NullZoneBlink : MonoBehaviour {

    public float flashOpacity = .6f;
    public float stayedFlashDuration = 0f;
    public float flashTransitionDuration = .1f;
    public Color flashColor;

    // Use this for initialization
    void Start () {
        GameModel.instance.notificationCenter.CallOnMessage(Message.NullChargePrevention, FlashNullZone);
    }

    void FlashNullZone() {
        if (this == null) {
            return;
        }
        var renderer = this.EnsureComponent<SpriteRenderer>();
        var startingColor = renderer.color;
        StartCoroutine(TransitionUtility.LerpColor(color => renderer.color = color,
                                                   renderer.color, flashColor, flashTransitionDuration));
        this.RealtimeDelayCall(() => FlashBackToNormal(startingColor), stayedFlashDuration+flashTransitionDuration);
    }

    void FlashBackToNormal(Color startingColor) {
        var renderer = GetComponent<SpriteRenderer>();
        if (renderer == null) {
            return;
        }

        StartCoroutine(TransitionUtility.LerpColor(color => renderer.color = color, renderer.color, startingColor, flashTransitionDuration));

    }
}
