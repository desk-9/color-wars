using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UtilityExtensions;

public class PlayerNullZoneEffect : MonoBehaviour {

    bool effectEnabled = false;
    bool inNullZone = false;
    Player player;
    new SpriteRenderer renderer;

    void Start() {
        player = GetComponent<Player>();
        renderer = GetComponent<SpriteRenderer>();
        GameModel.instance.nc.CallOnMessage(Message.BallIsPossessed, CheckEffect);
    }

    void CheckEffect() {
        var color = player?.team?.teamColor;
        var goal = GameModel.instance.goal;
        if (color != null && renderer != null && player != null
            && goal != null) {
            if (goal.currentTeam != player.team && inNullZone) {
                effectEnabled = true;
                var newColor = new Utility.HSVColor(color);
                newColor.v *= 0.85f;
                renderer.color = newColor.ToColor();
            } else {
                DisableEffect();
            }
        }
    }

    void DisableEffect() {
        var color = player?.team?.teamColor;
        if (color != null && renderer != null) {
            effectEnabled = false;
            renderer.color = color;
        }
    }

    void HandleEnter(Collider2D collider) {
        if (effectEnabled) {
            return;
        }
        var layer = collider.gameObject?.layer;
        if (layer.HasValue && layer.Value == LayerMask.NameToLayer("NullZone")) {
            inNullZone = true;
            CheckEffect();
        }
    }

    void OnTriggerEnter2D(Collider2D collider) {
        HandleEnter(collider);
    }

    void OnTriggerStay2D(Collider2D collider) {
        HandleEnter(collider);
    }

    void OnTriggerExit2D(Collider2D collider) {
        var layer = collider.gameObject?.layer;
        if (layer.HasValue && layer.Value == LayerMask.NameToLayer("NullZone")) {
            inNullZone = false;
            DisableEffect();
        }
    }
}
