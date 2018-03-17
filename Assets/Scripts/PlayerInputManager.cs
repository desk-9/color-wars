using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using InputDevice = InControl.InputDevice;
using InputManager = InControl.InputManager;


public class User {
    public InputDevice device;
    public TeamManager team;
    public User(InputDevice device) {
        this.device = device;
    }
}

// InputManager
public class PlayerInputManager : MonoBehaviour {

    public float deadzone = 0.25f;
    public static int maxPlayers { get { return 4; } }
    public delegate bool DevicePredicate(InputDevice inputDevice);

    
    Dictionary<InputDevice, User> users = new Dictionary<InputDevice, User>();

    // Keep track of input devices and their assignments.
    Dictionary<InputDevice, bool> devices = new Dictionary<InputDevice, bool>();

    // Registered actions to invoke on detach for each device.
    Dictionary<InputDevice, Action> actions = new Dictionary<InputDevice, Action>();

    public static PlayerInputManager instance;
    void Awake() {
        if (instance == null) {
            instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    void Start() {
        // Assign currently attached devices in order.
        foreach (var device in InputManager.Devices) {
            if (devices.Count >= 4) {
                Debug.LogFormat(this, "How are there more than 4 controllers?!");
                break;
            }

            Debug.LogFormat("{0}: Adding existing device {1} to list!", name, device.SortOrder);
            devices.Add(device, false);
        }

        // New device attached.
        InputManager.OnDeviceAttached += (device) => {
            Debug.LogFormat(this, "{0}: New device detected! Adding {1} to list.", name, device.SortOrder);

            devices.Add(device, false);
            users.Add(device, new User(device));
        };

        InputManager.OnDeviceDetached += (device) => {
            Debug.LogFormat(this, "{0}: Device {1} detached! Removing from list.", name, device.SortOrder);

            actions[device]();

            devices.Remove(device);
            actions.Remove(device);
            users.Remove(device);
        };
    }

    // Returns the next available input device; null if none is available.
    // Registers the action to be called when a device is detached (one-time only).
    public InputDevice GetInputDevice(Action action) {
        var e = devices.FirstOrDefault(ee => ee.Value == false);

        var device = e.Key;

        if (device == null) return null;

        devices[device] = true;
        actions[device] = action;

        return device;
    }

    public bool Any(DevicePredicate devicePredicate) {
        return InputManager.Devices.Any((device) => devicePredicate(device));
    }


    // Source: http://www.third-helix.com/2013/04/12/doing-thumbstick-dead-zones-right.html
    public Vector2 GetLeftStickInput(InputDevice device) {
        Vector2 stickInput = new Vector2(device.LeftStickX, device.LeftStickY);
        return ApplyRadialDeadzone(stickInput);
    }
    Vector2 ApplyScaledRadialDeadzone(Vector2 stickInput) {
        if (stickInput.magnitude < deadzone) {
            return Vector2.zero;
        }
        return stickInput.normalized * ((stickInput.magnitude - deadzone) / (1 - deadzone));
    }
    Vector2 ApplyRadialDeadzone(Vector2 stickInput) {
        if (stickInput.magnitude < deadzone) {
            return Vector2.zero;
        }
        return stickInput;
    }
    
}
