using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using InputDevice = InControl.InputDevice;
using InputManager = InControl.InputManager;

// InputManager
public class PlayerInputManager : MonoBehaviour {
    public static int maxPlayers { get { return 4; } }

    // Keep track of input devices and their assignments.
    Dictionary<InputDevice, bool> devices = new Dictionary<InputDevice, bool>();

    // Registered actions to invoke on detach for each device.
    Dictionary<InputDevice, Action> actions = new Dictionary<InputDevice, Action>();

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
        };

        // Device detached.
        InputManager.OnDeviceDetached += (device) => {
            Debug.LogFormat(this, "{0}: Removing device {1} from list.", name, device.SortOrder);
            devices.Remove(device);
        };
    }

    // Returns the next available input device; null if none is available.
    // Registers the action to be called when a device is detached (one-time only).
    public InputDevice GetInputDevice(Action action) {
        var e = devices.FirstOrDefault(ee => ee.Value == false);

        var device = e.Key;

        if (device == null) return null;

        devices[device] = true;
        actions.Add(device, action);
        InputManager.OnDeviceDetached += (d) => {
            if (d == device) {
                actions[d]();
                actions.Remove(d);
            }
        };

        return device;
    }
}
