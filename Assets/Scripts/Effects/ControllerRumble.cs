using UnityEngine;
using InControl;
using UtilityExtensions;

public class ControllerRumble : MonoBehaviour
{

    public bool rumbleEnabled = true;
    public float stealRumbleDuration = .3f;
    public float wallDestroyDuration = .2f;
    public float gameWinRumbleDuration = 1f;
    public float layingWallStunDuration = .5f;
    public float ballPossessionRumbleDuration = .2f;
    private PlayerControls playerControls;
    private PlayerStateManager stateManager;
    private int levelsOfRumble;

    // Use this for initialization
    private void Start()
    {
        if (!rumbleEnabled)
        {
            return;
        }
        playerControls = this.GetComponent<PlayerControls>();
        stateManager = GetComponent<PlayerStateManager>();
        if (playerControls != null && stateManager != null)
        {
            NotificationCenter nc = GameManager.instance.notificationCenter;
            nc.CallOnMessageIfSameObject(Message.StolenFrom, () => StartRumble(duration: stealRumbleDuration), gameObject);
            nc.CallOnMessageIfSameObject(Message.TronWallDestroyed,
                                         () => StartRumble(duration: wallDestroyDuration),
                                         gameObject);
            nc.CallOnMessage(Message.GoalScored, () => StartRumble(duration: gameWinRumbleDuration));
            nc.CallOnMessageIfSameObject(Message.TronWallDestroyedWhileLaying, () => StartRumble(duration: layingWallStunDuration), gameObject);
            stateManager.CallOnStateEnter(State.Posession, () => StartRumble(duration: ballPossessionRumbleDuration));
        }
    }

    private void StartRumble(float intensity = 1f, float? duration = null)
    {
        // If duration is null, this will rumble until StopRumble is called
        InputDevice inputDevice = playerControls?.GetInputDevice();
        if (inputDevice == null)
        {
            return;
        }
        levelsOfRumble += 1;
        inputDevice.Vibrate(intensity);
        if (duration.HasValue)
        {
            this.RealtimeDelayCall(StopRumble, duration.Value);
        }
    }

    private void StopRumble()
    {
        InputDevice inputDevice = playerControls?.GetInputDevice();
        levelsOfRumble -= 1;
        if (levelsOfRumble == 0 && inputDevice != null)
        {
            inputDevice.Vibrate(0f);
        }
    }

    private void OnDestroy()
    {
        StopRumble();
    }
}
