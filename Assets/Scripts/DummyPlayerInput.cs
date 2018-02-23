using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;

public class DummyPlayerInput : MonoBehaviour {

    public delegate void DeviceDisconnected();

    public InputDevice GetInputDevice(DeviceDisconnected callback)
    {
        return InputManager.ActiveDevice;
    }
}
