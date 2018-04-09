using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;
using UtilityExtensions;


public class PlayerControls : MonoBehaviour {

    InputDevice inputDevice;
    Coroutine broadcast;
    PlayerStateManager stateManager;

    void Start () {
        PlayerInputManager.instance.AddToInputQueue(GetComponent<Player>().playerNumber,
                                                    GivenInputDevice,
                                                    InputDeviceDisconnectedCallback);
        stateManager = GetComponent<PlayerStateManager>();
    }

    void GivenInputDevice(InputDevice device) {
        inputDevice = device;
        Debug.LogFormat("Player {1} acquired device {0}", inputDevice.SortOrder, this.name);
        var puppet = GetComponent<PlayerPuppet>();
        if (puppet == null || !puppet.doPuppeting) {
            broadcast = StartCoroutine(ControlsBroadcast());
        }
    }

    public InputDevice GetInputDevice() {
        return inputDevice;
    }

    void InputDeviceDisconnectedCallback() {
        Debug.LogFormat(this, "{0}: Input Device Disconnected", name);
        var movement = GetComponent<PlayerMovement>();
        movement?.StopAllMovement();

        if (broadcast != null) {
            StopCoroutine(broadcast);
        }
        broadcast = null;
        stateManager?.AttemptStartState(delegate{}, delegate{});
        inputDevice = null;
    }

    IEnumerator ControlsBroadcast() {
        while (true) {
            if (inputDevice == null) {
                yield return null;
                continue;
            }

            SendInputEvents(inputDevice.LeftStickX, inputDevice.LeftStickY,
                            inputDevice.Action1.WasPressed,
                            inputDevice.Action1.WasReleased,
                            inputDevice.Action2.WasPressed,
                            inputDevice.Action2.WasReleased,
                            this.gameObject);

            if (inputDevice.LeftBumper.WasPressed) {
                GameModel.instance.nc.NotifyMessage(
                    Message.PlayerPressedLeftBumper, this.gameObject);
            }
            if (inputDevice.LeftBumper.WasReleased) {
                GameModel.instance.nc.NotifyMessage(
                    Message.PlayerReleasedLeftBumper, this.gameObject);
            }

            if (inputDevice.RightBumper.WasPressed) {
                GameModel.instance.nc.NotifyMessage(
                    Message.PlayerPressedRightBumper, this.gameObject);
            }
            if (inputDevice.RightBumper.WasReleased) {
                GameModel.instance.nc.NotifyMessage(
                    Message.PlayerReleasedRightBumper, this.gameObject);
            }

            if (inputDevice.Action3.WasPressed) {
                GameModel.instance.nc.NotifyMessage(
                    Message.PlayerPressedX, this.gameObject);
            }
            if (inputDevice.Action3.WasReleased) {
                GameModel.instance.nc.NotifyMessage(
                    Message.PlayerReleasedX, this.gameObject);
            }

            yield return null;
        }
    }

    public static void SendInputEvents(float stickX, float stickY, bool APressed,
                                       bool AReleased, bool BPressed, bool BReleased,
                                       GameObject player) {
        if (player == null) {
            return;
        }
        GameModel.instance.nc.NotifyMessage(
            Message.PlayerStick,
            Tuple.Create(new Vector2(stickX, stickY), player));

        if (APressed) {
            GameModel.instance.nc.NotifyMessage(
                Message.PlayerPressedA, player);
            GameModel.instance.nc.NotifyMessage(
                Message.PlayerPressedDash, player);
            GameModel.instance.nc.NotifyMessage(
                Message.PlayerPressedShoot, player);
        }
        if (AReleased) {
            GameModel.instance.nc.NotifyMessage(
                Message.PlayerReleasedA, player);
            GameModel.instance.nc.NotifyMessage(
                Message.PlayerReleasedDash, player);
            GameModel.instance.nc.NotifyMessage(
                Message.PlayerReleasedShoot, player);
        }

        if (BPressed) {
            GameModel.instance.nc.NotifyMessage(
                Message.PlayerPressedB, player);
            GameModel.instance.nc.NotifyMessage(
                Message.PlayerPressedWall, player);
        }
        if (BReleased) {
            GameModel.instance.nc.NotifyMessage(
                Message.PlayerReleasedB, player);
            GameModel.instance.nc.NotifyMessage(
                Message.PlayerReleasedWall, player);
        }
    }


    void OnDestroy() {
        if (inputDevice != null) {
            PlayerInputManager.instance.devices[inputDevice] = false;
            PlayerInputManager.instance.actions[inputDevice] = delegate{};
        }
    }
}
