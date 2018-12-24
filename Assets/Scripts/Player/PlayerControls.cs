using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IC = InControl;

using UtilityExtensions;
using Photon.Realtime;
using Photon.Pun;

public struct ButtonEventPair
{
    public Message pressedEvent;
    public Message releasedEvent;
    public ButtonEventPair(Message pressedEvent, Message releasedEvent)
    {
        this.pressedEvent = pressedEvent;
        this.releasedEvent = releasedEvent;
    }
}

public class PlayerControls : MonoBehaviourPunCallbacks
{
    private IC.InputDevice inputDevice;
    private Coroutine broadcast;
    private PlayerStateManager stateManager;

    private void Start()
    {
        stateManager = GetComponent<PlayerStateManager>();

    }

    public void AskForDevice() {
        var player = GetComponent<Player>();
        var photonView = GetComponent<PhotonView>();
        if (photonView.IsMine) {
            PlayerInputManager.instance.AddToInputQueue(GetComponent<Player>().playerNumber,
                                                        GivenInputDevice,
                                                        InputDeviceDisconnectedCallback);
        }
    }

    private void GivenInputDevice(IC.InputDevice device)
    {
        inputDevice = device;
        GameManager.Instance.NotificationManager.NotifyMessage(Message.InputDeviceAssigned, gameObject);
        PlayerPuppet puppet = GetComponent<PlayerPuppet>();
        if (puppet == null || !puppet.doPuppeting)
        {
            broadcast = StartCoroutine(ControlsBroadcast());
        }
    }

    public IC.InputDevice GetInputDevice()
    {
        return inputDevice;
    }

    private void InputDeviceDisconnectedCallback()
    {
        // TODO dkonik: Since we are making a public game now, we need to do more on
        // controller disconnect than just stopping movement and stuff.
        //PlayerMovement movement = GetComponent<PlayerMovement>();
        //movement?.StopAllMovement();

        if (broadcast != null)
        {
            StopCoroutine(broadcast);
        }
        broadcast = null;
        stateManager.TransitionToState(State.StartupState);
        inputDevice = null;
    }

    private Dictionary<IC.InputControlType, ButtonEventPair> buttonEvents =
        new Dictionary<IC.InputControlType, ButtonEventPair>()
        {

            [IC.InputControlType.LeftBumper] = new ButtonEventPair(
            Message.PlayerPressedLeftBumper, Message.PlayerReleasedLeftBumper),
            [IC.InputControlType.RightBumper] = new ButtonEventPair(
            Message.PlayerPressedRightBumper, Message.PlayerReleasedRightBumper),
            [IC.InputControlType.Action3] = new ButtonEventPair(
            Message.PlayerPressedX, Message.PlayerReleasedX),
            [IC.InputControlType.Action4] = new ButtonEventPair(
            Message.PlayerPressedY, Message.PlayerReleasedY),
            [IC.InputControlType.Back] = new ButtonEventPair(
            Message.PlayerPressedBack, Message.PlayerReleasedBack),
        };

    private IEnumerator ControlsBroadcast()
    {
        while (true)
        {
            if (inputDevice == null)
            {
                yield return null;
                continue;
            }

            SendInputEvents(inputDevice.LeftStickX, inputDevice.LeftStickY,
                            inputDevice.Action1.WasPressed,
                            inputDevice.Action1.WasReleased,
                            inputDevice.Action2.WasPressed,
                            inputDevice.Action2.WasReleased,
                            this.gameObject);

            foreach (KeyValuePair<IC.InputControlType, ButtonEventPair> kvPair in buttonEvents)
            {
                CheckButtonEvents(kvPair.Key, inputDevice, gameObject,
                                  kvPair.Value.pressedEvent, kvPair.Value.releasedEvent);
            }
            yield return null;
        }
    }

    public static void CheckButtonEvents(IC.InputControlType type,
                                         IC.InputDevice device,
                                         GameObject player,
                                         Message? pressedEvent = null,
                                         Message? releasedEvent = null)
    {
        if (device != null)
        {
            IC.InputControl button = device.GetControl(type);
            SendButtonEvent(button.WasPressed, button.WasReleased,
                            player, pressedEvent, releasedEvent);
        }
    }

    public static void SendButtonEvent(bool pressed, bool released,
                                       GameObject player,
                                       Message? pressedEvent = null,
                                       Message? releasedEvent = null)
    {
        if (pressed && pressedEvent.HasValue)
        {
            GameManager.Instance.NotificationManager.NotifyMessage(pressedEvent.Value, player);
        }
        if (released && releasedEvent.HasValue)
        {
            GameManager.Instance.NotificationManager.NotifyMessage(releasedEvent.Value, player);
        }
    }

    public static void SendInputEvents(float stickX, float stickY, bool APressed,
                                       bool AReleased, bool BPressed, bool BReleased,
                                       GameObject player)
    {
        if (player == null)
        {
            return;
        }
        GameManager.Instance.NotificationManager.NotifyMessage(
            Message.PlayerStick,
            Tuple.Create(new Vector2(stickX, stickY), player));

        List<ButtonEventPair> AEvents = new List<ButtonEventPair>() {
            new ButtonEventPair(Message.PlayerPressedA, Message.PlayerReleasedA),
            new ButtonEventPair(Message.PlayerPressedDash, Message.PlayerReleasedDash),
            new ButtonEventPair(Message.PlayerPressedShoot, Message.PlayerReleasedShoot),
        };
        foreach (ButtonEventPair events in AEvents)
        {
            SendButtonEvent(APressed, AReleased, player,
                            events.pressedEvent, events.releasedEvent);
        }
        List<ButtonEventPair> BEvents = new List<ButtonEventPair>() {
            new ButtonEventPair(Message.PlayerPressedB, Message.PlayerReleasedB),
            new ButtonEventPair(Message.PlayerPressedWall, Message.PlayerReleasedWall),
        };
        foreach (ButtonEventPair events in BEvents)
        {
            SendButtonEvent(BPressed, BReleased, player,
                            events.pressedEvent, events.releasedEvent);
        }
    }

    private void OnDestroy()
    {
        if (inputDevice != null)
        {
            PlayerInputManager.instance.devices[inputDevice] = false;
            PlayerInputManager.instance.actions[inputDevice] = delegate { };
        }
    }
}
