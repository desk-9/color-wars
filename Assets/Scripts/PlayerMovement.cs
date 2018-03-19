using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;
using UtilityExtensions;


public class PlayerMovement : MonoBehaviour {

    public float movementSpeed;
    public bool freezeRotation = false;

    Rigidbody2D rb2d;
    InputDevice inputDevice;
    Coroutine playerMovementCoroutine = null;
    PlayerStateManager stateManager;

    void StartPlayerMovement() {
        playerMovementCoroutine = StartCoroutine(Move());
    }

    void StopAllMovement() {
        if (playerMovementCoroutine != null) {
            StopCoroutine(playerMovementCoroutine);
            rb2d.velocity = Vector2.zero;
        }
    }

    public void FreezePlayer() {
        rb2d.isKinematic = true;
    }

    public void UnFreezePlayer() {
        rb2d.isKinematic = false;
    }

    public void RotatePlayer () {
        if (inputDevice == null || freezeRotation) {
            return;
        }
        var direction = new Vector2(inputDevice.LeftStickX, inputDevice.LeftStickY);
        if (direction != Vector2.zero) {
            // Only do if nonzero, otherwise [SignedAngle] returns 90 degrees
            // and player snaps to up direction
            rb2d.rotation = Vector2.SignedAngle(Vector2.right, direction);
        }
    }

    IEnumerator Move () {
        if (inputDevice == null) {
            yield break;
        }
        float startTime = Time.time;
        yield return new WaitForFixedUpdate();
        while (true) {
            var direction = new Vector2(inputDevice.LeftStickX, inputDevice.LeftStickY);
            rb2d.velocity = movementSpeed * direction;
            // TODO: TUTORIAL
            if (direction.magnitude > 0.1f) {
                if (Time.time - startTime > 0.75f) {
                    Debug.Log("Notifying!");
                    GameModel.instance.nc.NotifyStringEvent("MoveTutorial", this.gameObject);
                }
            } else {
                startTime = Time.time;
            }

            RotatePlayer();
            yield return new WaitForFixedUpdate();
        }
    }

    // Use this for initialization
    void Start () {
        rb2d = this.EnsureComponent<Rigidbody2D>();
        stateManager = this.EnsureComponent<PlayerStateManager>();

        TryToGetInputDevice();
    }

    void TryToGetInputDevice() {
        inputDevice = PlayerInputManager.instance.GetInputDevice(
            InputDeviceDisconnectedCallback);
        if (inputDevice != null) {
            Debug.LogFormat("Acquired device {0}", GetComponent<Player>().team.teamNumber);
            stateManager.AttemptNormalMovement(StartPlayerMovement, StopAllMovement);
        }
    }

    public InputDevice GetInputDevice() {
        return inputDevice;
    }

    void InputDeviceDisconnectedCallback() {
        Debug.LogFormat(this, "{0}: Input Device Disconnected", name);
        StopAllMovement();
        stateManager.AttemptStartState(delegate{}, delegate{});
        inputDevice = null;
    }

    void Update() {
        if (inputDevice == null) {
            TryToGetInputDevice();
        }
    }

    void OnDestroy() {
        if (inputDevice != null) {
            Debug.Log("destroyed");
            PlayerInputManager.instance.devices[inputDevice] = false;
            PlayerInputManager.instance.actions[inputDevice] = delegate{};
        }
    }
}
