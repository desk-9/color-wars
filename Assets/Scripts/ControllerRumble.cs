using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;
using UtilityExtensions;

public class ControllerRumble : MonoBehaviour {

    public bool rumbleEnabled = true;
    public float stealRumbleDuration = .3f;
    public float wallDestroyRumbleIntensity = .5f;
    public float wallDestroyDuration = .2f;
    public float gameWinRumbleDuration = 1f;
    public float layingWallStunDuration = .5f;

    IPlayerMovement playerMovement;
    PlayerStateManager stateManager;
    InputDevice inputDevice;
    int levelsOfRumble;

    // Use this for initialization
    void Start () {
        if (!rumbleEnabled) {
            return;
        }
        playerMovement = GetComponent<IPlayerMovement>();
        stateManager = GetComponent<PlayerStateManager>();
        if (playerMovement != null && stateManager != null) {
            var nc = GameModel.instance.nc;
            nc.CallOnMessageIfSameObject(Message.InputDeviceAssigned, AssignInputDevice, gameObject);
            nc.CallOnMessageIfSameObject(Message.StealOccurred, () => StartRumble(duration : stealRumbleDuration), gameObject);
            nc.CallOnMessageIfSameObject(Message.TronWallDestroyed,
                                         () => StartRumble(intensity : wallDestroyRumbleIntensity, duration : wallDestroyDuration),
                                         gameObject);
            nc.CallOnMessage(Message.GoalScored, () => StartRumble(duration : gameWinRumbleDuration));
            nc.CallOnMessageIfSameObject(Message.TronWallDestroyedWhileLaying, () => StartRumble(duration : layingWallStunDuration), gameObject);
        }
    }

    void AssignInputDevice() {
        inputDevice = this.EnsureComponent<PlayerControls>().GetInputDevice();
    }

    void StartRumble(float intensity = 1f, float? duration = null) {
        levelsOfRumble += 1;
        inputDevice.Vibrate(intensity);
        if (duration.HasValue) {
            this.RealtimeDelayCall(StopRumble, duration.Value);
        }
    }

    void StopRumble() {
        levelsOfRumble -= 1;
        if (levelsOfRumble == 0) {
            inputDevice.Vibrate(0f);
        }
    }
}
