using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UtilityExtensions;
using InputDevice = InControl.InputDevice;
using InputManager = InControl.InputManager;


public class User
{
    public InputDevice device;
    public TeamManager team;
    public User(InputDevice device)
    {
        this.device = device;
    }
}

public delegate void InputDeviceCallback(InputDevice device);

// InputManager
public class PlayerInputManager : MonoBehaviour
{

    public float deadzone = 0.25f;
    public static int maxPlayers { get { return 4; } }
    public delegate bool DevicePredicate(InputDevice inputDevice);

    // Keep track of input devices and their assignments.
    public Dictionary<InputDevice, bool> devices = new Dictionary<InputDevice, bool>();

    // Registered actions to invoke on detach for each device.
    public Dictionary<InputDevice, Action> actions = new Dictionary<InputDevice, Action>();

    public static PlayerInputManager instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Assign currently attached devices in order.
        foreach (InputDevice device in InputManager.Devices)
        {
            if (devices.Count >= 4)
            {
                break;
            }
            devices.Add(device, false);
        }

        // New device attached.
        InputManager.OnDeviceAttached += (device) =>
        {
            devices.Add(device, false);
            this.FrameDelayCall(() =>
            {
                HandoutDevices();
            }, 1);
        };

        InputManager.OnDeviceDetached += (device) =>
        {
            if (actions.ContainsKey(device))
            {
                actions[device]();
            }

            devices.Remove(device);
            actions.Remove(device);
        };

        this.FrameDelayCall(() =>
        {
            HandoutDevices();
        }, 2);
        SceneManager.sceneLoaded += OnLevelLoaded;
    }

    private void OnLevelLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
    {
        foreach (InputDevice k in devices.Keys.ToList())
        {
            devices[k] = false;
        }
    }

    private SortedList<int, Tuple<InputDeviceCallback, Action>> inputRequests =
        new SortedList<int, Tuple<InputDeviceCallback, Action>>();

    public void AddToInputQueue(int priority, InputDeviceCallback callback, Action action)
    {
        inputRequests.Add(priority, Tuple.Create(callback, action));
        HandoutDevices();
    }

    private void HandoutDevices()
    {
        if (!inputRequests.Any())
        {
            return;
        }
        IEnumerable<InputDevice> unusedDevices =
            from pair in devices
            where pair.Key != null && !pair.Value
            select pair.Key;
        List<InputDevice> sortedDevices = unusedDevices.OrderBy(d => d.SortOrder).ToList();
        if (sortedDevices.Any())
        {
            // This int is outside the loop because it must be used to remove
            // all fulfilled requests (if there are more requests than devices,
            // i will go up to the index of the first request left unfulfilled)
            int i = 0;
            for (; i < sortedDevices.Count && i < inputRequests.Count; i++)
            {
                InputDevice device = sortedDevices[i];
                InputDeviceCallback createdCallback = inputRequests.Values[i].Item1;
                Action action = inputRequests.Values[i].Item2;
                devices[device] = true;
                actions[device] = action;
                createdCallback(device);
            }
            Dictionary<int, Tuple<InputDeviceCallback, Action>> unfufilledRequestsDictionary = inputRequests.Skip(i).ToDictionary(
                pair => pair.Key, pair => pair.Value);
            inputRequests = new SortedList<int, Tuple<InputDeviceCallback, Action>>(unfufilledRequestsDictionary);
        }
    }

    // Returns the next available input device; null if none is available.
    // Registers the action to be called when a device is detached (one-time only).
    public InputDevice GetInputDevice(Action action)
    {
        KeyValuePair<InputDevice, bool> e = devices.FirstOrDefault(ee => ee.Value == false);

        InputDevice device = e.Key;
        if (device == null) return null;

        devices[device] = true;
        actions[device] = action;

        return device;
    }

    public bool Any(DevicePredicate devicePredicate)
    {
        return devices.Any(
            (devicePair) =>
            {
                if (devicePair.Key != null)
                {
                    return devicePredicate(devicePair.Key);
                }
                else
                {
                    return false;
                }
            });
    }


    // Source: http://www.third-helix.com/2013/04/12/doing-thumbstick-dead-zones-right.html
    public Vector2 GetLeftStickInput(InputDevice device)
    {
        Vector2 stickInput = new Vector2(device.LeftStickX, device.LeftStickY);
        return ApplyRadialDeadzone(stickInput);
    }

    private Vector2 ApplyScaledRadialDeadzone(Vector2 stickInput)
    {
        if (stickInput.magnitude < deadzone)
        {
            return Vector2.zero;
        }
        return stickInput.normalized * ((stickInput.magnitude - deadzone) / (1 - deadzone));
    }

    private Vector2 ApplyRadialDeadzone(Vector2 stickInput)
    {
        if (stickInput.magnitude < deadzone)
        {
            return Vector2.zero;
        }
        return stickInput;
    }

}
