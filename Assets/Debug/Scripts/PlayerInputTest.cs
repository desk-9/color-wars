using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

[RequireComponent(typeof(PlayerInputManager))]
public class PlayerInputTest : MonoBehaviour {
    PlayerInputManager pim;

    InputDevice device;

    void Awake() {
        pim = GetComponent<PlayerInputManager>();
    }

    void Update() {
        if (device == null) {
            device = pim.GetInputDevice(RemoveDevice);

            if (device != null) {
                Debug.LogFormat(this, "{0}: Device {1} acquired!", name, device.SortOrder);
            }

            return;
        }

        if (device.Action1.WasPressed) Bark();
    }

    void RemoveDevice() {
        Debug.LogFormat(this, "{0}: Device {1} removed!", name, device.SortOrder);
        device = null;
    }

    void Bark() {
        Debug.LogFormat(this, "{0}: BARK by {1}!", name, device.SortOrder);
    }
}
