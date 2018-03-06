using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;


public class PlayerMovement : MonoBehaviour {

    public float movementSpeed;

    Rigidbody2D rb2d;
    InputDevice inputDevice;
    Coroutine playerMovementCoroutine = null;
    PlayerInputManager playerInput;
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

    public void RotatePlayer () {
	if (inputDevice == null) {
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
	
        yield return new WaitForFixedUpdate();
        while (true) {
            var direction = new Vector2(inputDevice.LeftStickX, inputDevice.LeftStickY);
            rb2d.velocity = movementSpeed * direction;

	    RotatePlayer();
            yield return new WaitForFixedUpdate();
        }
    }

    // Use this for initialization
    void Start () {
        rb2d = GetComponent<Rigidbody2D>();
        playerInput = GameModel.instance.GetComponent<PlayerInputManager>();
	stateManager = GetComponent<PlayerStateManager>();

        TryToGetInputDevice();
    }

    void TryToGetInputDevice() {
        inputDevice = playerInput.GetInputDevice(InputDeviceDisconnectedCallback);
        if (inputDevice != null) {
	    stateManager.AttemptNormalMovement(StartPlayerMovement, StopAllMovement);
        }
    }

    public InputDevice GetInputDevice() {
        return inputDevice;
    }

    void InputDeviceDisconnectedCallback() {
        Debug.LogFormat(this, "{0}: Input Device Disconnected", name);
        StopAllMovement();
        inputDevice = null;
    }

    void Update() {
        if (inputDevice == null) {
            TryToGetInputDevice();
        }
    }
}
