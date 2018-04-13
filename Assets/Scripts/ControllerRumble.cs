using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;
using UtilityExtensions;

public class ControllerRumble : MonoBehaviour {

    public float durationOfStealRumble = .3f;

    IPlayerMovement playerMovement;
    PlayerStateManager stateManager;
    InputDevice inputDevice;

    // Use this for initialization
    void Start () {
        playerMovement = GetComponent<IPlayerMovement>();
        stateManager = GetComponent<PlayerStateManager>();
        if (playerMovement != null && stateManager != null) {
            GameModel.instance.nc.CallOnMessageIfSameObject(Message.InputDeviceAssigned, AssignInputDevice, gameObject);
            GameModel.instance.nc.CallOnMessageIfSameObject(Message.StealOccurred, () => StartRumble(duration : durationOfStealRumble), gameObject);
        }
    }

    void AssignInputDevice() {
        inputDevice = this.EnsureComponent<PlayerControls>().GetInputDevice();
    }

    void StartRumble(float intensity = 1f, float? duration = null) {
        inputDevice.Vibrate(intensity);
        if (duration.HasValue) {
            this.RealtimeDelayCall(StopRumble, duration.Value);
        }
    }

    void StopRumble() {
        inputDevice.Vibrate(0f);
    }
}
