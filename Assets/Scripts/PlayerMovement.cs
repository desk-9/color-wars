using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InControl;


public class PlayerMovement : MonoBehaviour {

    public float movementSpeed;
    public Callback StartMovementFunction = delegate {};
    public Callback StopMovementFunction = delegate {};

    Rigidbody2D rb2d;
    InputDevice inputDevice;
    Coroutine playerMovementCoroutine = null;
    PlayerInputManager playerInput;

    public void StartPlayerMovement()
    {
        playerMovementCoroutine = StartCoroutine(Move());
        StartMovementFunction();
    }

    public void ParalyzePlayer(float timePeriod)
    {
        StartCoroutine(PausePlayerMovement(timePeriod));
    }

    public IEnumerator PausePlayerMovement(float timePeriod)
    {
        StopAllMovement();
        yield return new WaitForSeconds(timePeriod);
        StartPlayerMovement();
    }

    public void StopAllMovement()
    {
	if (playerMovementCoroutine != null) {
	    StopCoroutine(playerMovementCoroutine);
	    rb2d.velocity = Vector2.zero;
            StopMovementFunction();
	}
    }

    public void RotatePlayer() {
	if (inputDevice != null) {   
	    var direction = new Vector2(inputDevice.LeftStickX, inputDevice.LeftStickY);
	    if (direction != Vector2.zero) {
		rb2d.rotation = Vector2.SignedAngle(Vector2.right, direction);
	    }
	}
    }

    // Handles players movement on the game board
    IEnumerator Move ()
    {
	if (inputDevice == null) {
	    yield break;
	}
	
        yield return new WaitForFixedUpdate();
        while (true) {
            var direction = new Vector2(inputDevice.LeftStickX, inputDevice.LeftStickY);
            rb2d.velocity = movementSpeed * direction;

            // Only do if nonzero, otherwise [SignedAngle] returns 90 degrees
            // and player snaps to up direction
            if (direction != Vector2.zero) {
                rb2d.rotation = Vector2.SignedAngle(Vector2.right, direction);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    // Use this for initialization
    void Start () {
        rb2d = GetComponent<Rigidbody2D>();
        playerInput = GameModel.instance.GetComponent<PlayerInputManager>();

        TryToGetInputDevice();
    }

    void TryToGetInputDevice()
    {
        inputDevice = playerInput.GetInputDevice(InputDeviceDisconnectedCallback);
        if (inputDevice != null) {
            StartPlayerMovement();
        }
    }

    public InputDevice GetInputDevice() {
        return inputDevice;
    }

    void InputDeviceDisconnectedCallback()
    {
        Debug.LogFormat(this, "{0}: Input Device Disconnected", name);
        StopAllMovement();
        inputDevice = null;
    }

    void Update()
    {
        if (inputDevice == null) {
            TryToGetInputDevice();
        }
    }
}
