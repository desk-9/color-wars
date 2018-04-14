using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;
using UtilityExtensions;

public class ControllerRumble : MonoBehaviour {

    public bool rumbleEnabled = true;
    public float stealRumbleDuration = .3f;
    public float wallDestroyDuration = .2f;
    public float gameWinRumbleDuration = 1f;
    public float layingWallStunDuration = .5f;
    public float ballPossessionRumbleDuration = .2f;

    PlayerControls playerControls;
    PlayerStateManager stateManager;
    int levelsOfRumble;

    // Use this for initialization
    void Start () {
        if (!rumbleEnabled) {
            return;
        }
        playerControls = this.GetComponent<PlayerControls>();
        stateManager = GetComponent<PlayerStateManager>();
        if (playerControls != null && stateManager != null) {
            var nc = GameModel.instance.nc;
            nc.CallOnMessageIfSameObject(Message.StealOccurred, () => StartRumble(duration : stealRumbleDuration), gameObject);
            nc.CallOnMessageIfSameObject(Message.TronWallDestroyed,
                                         () => StartRumble(duration : wallDestroyDuration),
                                         gameObject);
            nc.CallOnMessage(Message.GoalScored, () => StartRumble(duration : gameWinRumbleDuration));
            nc.CallOnMessageIfSameObject(Message.TronWallDestroyedWhileLaying, () => StartRumble(duration : layingWallStunDuration), gameObject);
            stateManager.CallOnStateEnter(State.Posession, () => StartRumble(duration : ballPossessionRumbleDuration));
        }
    }

    void StartRumble(float intensity = 1f, float? duration = null) {
        var inputDevice = playerControls?.GetInputDevice();
        if (inputDevice == null) {
            return;
        }
        levelsOfRumble += 1;
        inputDevice.Vibrate(intensity);
        if (duration.HasValue) {
            this.RealtimeDelayCall(StopRumble, duration.Value);
        }
    }

    void StopRumble() {
        var inputDevice = playerControls?.GetInputDevice();
        levelsOfRumble -= 1;
        if (levelsOfRumble == 0 && inputDevice != null) {
            inputDevice.Vibrate(0f);
        }
    }
}
